using System.Text.RegularExpressions;
using AutoTag.Core.Files;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;

namespace AutoTag.Core.TV;
public class TVProcessor(TMDbClient tmdb, IFileWriter writer, IUserInterface ui, AutoTagConfig config) : IProcessor
{
    private readonly Dictionary<string, List<ShowResults>> CachedShows = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(int, int), TvSeason> CachedSeasons = new();
    private readonly Dictionary<(string, int), string> CachedSeasonPosters = new();
    private IEnumerable<Genre> Genres = [];

    public async Task<bool> ProcessAsync(TaggingFile file)
    {
        var fileNameData = ParseFileName(file);
        if (fileNameData == null)
        {
            return false;
        }

        ui.SetStatus($"Parsed file as {fileNameData}", MessageType.Log);

        var findShowResult = await FindShowAsync(fileNameData.SeriesName);
        switch (findShowResult)
        {
            case FindResult.Fail:
                return false;
            case FindResult.Skip:
                return true;
        }
        
        var result = new TVFileMetadata
        {
            Season = fileNameData.Season,
            Episode = fileNameData.Episode
        };
        
        // try searching for each series search result
        foreach (var show in CachedShows[fileNameData.SeriesName])
        {
            var findEpisodeResult = await FindEpisodeAsync(fileNameData, show, result, file.Taggable);
            if (findEpisodeResult == FindResult.Fail)
            {
                return false;
            }
            else if (findEpisodeResult == FindResult.Success)
            {
                break;
            }
        }

        ui.SetStatus($"Found {fileNameData} ({result.Title}) on TheMovieDB", MessageType.Information);

        if (config.AddCoverArt && string.IsNullOrEmpty(result.CoverURL) && file.Taggable)
        {
            await FindPosterAsync(result, ui);
        }

        var taggingSuccess = await writer.WriteAsync(file, result);

        return taggingSuccess && result.Success && result.Complete;
    }

    private TVFileMetadata? ParseFileName(TaggingFile file)
    {
        TVFileMetadata episodeData;

        if (string.IsNullOrEmpty(config.ParsePattern))
        {
            try
            {
                episodeData = EpisodeParser.ParseEpisodeInfo(Path.GetFileName(file.Path)); // Parse info from filename
            }
            catch (FormatException ex)
            {
                ui.SetStatus($"Error: {ex.Message}", MessageType.Error);
                return null;
            }
        }
        else
        {
            try
            {
                var match = Regex.Match(Path.GetFullPath(file.Path), config.ParsePattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

                episodeData = new TVFileMetadata
                {
                    SeriesName = match.Groups["SeriesName"].Value,
                    Season = int.Parse(match.Groups["Season"].Value),
                    Episode = int.Parse(match.Groups["Episode"].Value)
                };
            }
            catch (FormatException ex)
            {
                if (config.Verbose)
                {
                    ui.SetStatus($"Error: Unable to parse required information from filename ({ex.GetType().Name}: {ex.Message})", MessageType.Error);
                }
                else
                {
                    ui.SetStatus($"Error: Unable to parse required information from filename", MessageType.Error);
                }

                return null;
            }
        }

        return episodeData;
    }

    private async Task<FindResult> FindShowAsync(string seriesName)
    {
        if (!CachedShows.ContainsKey(seriesName))
        {
            // if not already searched for series
            SearchContainer<SearchTv> searchResults = await tmdb.SearchTvShowAsync(seriesName);

            var seriesResults = searchResults.Results
                .OrderByDescending(searchResult => SeriesNameSimilarity(seriesName, searchResult.Name))
                .ToList();

            // using episode groups, requires the manual selection of a show
            if (config.ManualMode || config.EpisodeGroup)
            {
                var chosen = ui.SelectOption(
                    "Please choose an option:",
                    seriesResults
                        .Select(t => $"{t.Name} ({t.FirstAirDate?.Year.ToString() ?? "Unknown"})")
                        .ToList()
                );

                if (chosen.HasValue)
                {
                    var chosenSeries = seriesResults[chosen.Value];
                    CachedShows.Add(seriesName, [chosenSeries]);
                    ui.SetStatus($"Selected {chosenSeries.Name} ({chosenSeries.FirstAirDate?.Year.ToString() ?? "Unknown"})", MessageType.Information);
                }
                else
                {
                    ui.SetStatus("File skipped", MessageType.Warning);
                    return FindResult.Skip;
                }
            }
            else if (seriesResults.Count == 0)
            {
                ui.SetStatus($"Error: Cannot find series {seriesName} on TheMovieDB", MessageType.Error);
                return FindResult.Fail;
            }
            else
            {
                CachedShows.Add(seriesName, ShowResults.FromSearchResults(seriesResults));
            }

            if (config.EpisodeGroup)
            {
                var seriesResult = CachedShows[seriesName][0];
                var tvShow = await tmdb.GetTvShowAsync(seriesResult.TvSearchResult.Id, TvShowMethods.EpisodeGroups);
                var groups = tvShow.EpisodeGroups;

                if (groups.Results.Count != 0)
                {
                    var chosenGroup = ui.SelectOption(
                        "Please choose an episode ordering:",
                        groups.Results
                            .Select(g => $"[{g.Type}] {g.Name} ({g.GroupCount} seasons, {g.EpisodeCount} episodes)")
                            .ToList()
                    );

                    if (chosenGroup.HasValue)
                    {
                        var groupInfo = await tmdb.GetTvEpisodeGroupsAsync(groups.Results[chosenGroup.Value].Id, config.Language);
                        if (groupInfo is null)
                        {
                            ui.SetStatus($@"Error: Could not retrieve TV episode groups for show ""{tvShow.Name}""", MessageType.Error);
                            return FindResult.Fail;
                        }

                        if (seriesResult.AddEpisodeGroup(groupInfo))
                        {
                            ui.SetStatus($"Selected {groupInfo.Name} episode ordering", MessageType.Information);
                        }
                        else
                        {
                            ui.SetStatus($@"Error: Unable to generate a unique season-episode mapping for collection group ""{groupInfo.Name}""", MessageType.Error);
                            return FindResult.Fail;
                        }
                        
                        
                    }
                    else
                    {
                        ui.SetStatus("File skipped", MessageType.Warning);
                        return FindResult.Skip;
                    }
                }
                else
                {
                    ui.SetStatus("No episode groups found for series", MessageType.Warning);
                }
            }
        }

        return FindResult.Success;
    }

    private async Task<FindResult> FindEpisodeAsync(TVFileMetadata fileNameData, ShowResults show, TVFileMetadata result, bool fileIsTaggable)
    {
        var showData = show.TvSearchResult;
        
        var lookupSeason = fileNameData.Season;
        var lookupEpisode = fileNameData.Episode;

        if (show.EpisodeGroupMappingTable != null)
        {
            if (show.TryGetMapping(fileNameData.Season, fileNameData.Episode, out var groupNumbering))
            {
                lookupSeason = groupNumbering.Value.season;
                lookupEpisode = groupNumbering.Value.episode;
            }
            else
            {
                ui.SetStatus($"Error: Cannot find {fileNameData} in episode group on TheMovieDB", MessageType.Error);
                return FindResult.Fail;
            }
        }

        result.Id = showData.Id;
        result.SeriesName = showData.Name;

        if (!CachedSeasons.TryGetValue((showData.Id, lookupSeason), out var seasonResult))
        {
            seasonResult = await tmdb.GetTvSeasonAsync(showData.Id, lookupSeason);

            if (seasonResult == null)
            {
                if (showData.Id == CachedShows[fileNameData.SeriesName][^1].TvSearchResult.Id)
                {
                    ui.SetStatus($"Error: Cannot find {fileNameData} on TheMovieDB", MessageType.Error);
                    return FindResult.Fail;
                }
                
                return FindResult.Skip;
            }
            
            CachedSeasons.Add((showData.Id, lookupSeason), seasonResult);
        }
        
        result.SeasonEpisodes = seasonResult.Episodes.Count;

        if (!string.IsNullOrEmpty(seasonResult.PosterPath))
        {
            result.CoverURL = $"https://image.tmdb.org/t/p/original/{seasonResult.PosterPath}";
        }

        var episodeResult = seasonResult.Episodes.Find(e => e.EpisodeNumber == lookupEpisode);
        if (episodeResult == default)
        {
            if (showData.Id == CachedShows[fileNameData.SeriesName][^1].TvSearchResult.Id)
            {
                ui.SetStatus($"Error: Cannot find {fileNameData} on TheMovieDB", MessageType.Error);

                return FindResult.Fail;
            }
            
            return FindResult.Skip;
        }

        result.Title = episodeResult.Name;
        result.Overview = episodeResult.Overview;
        
        if (!Genres.Any())
        {
            Genres = await tmdb.GetTvGenresAsync();
        }
        result.Genres = show.TvSearchResult.GenreIds.Select(gId => Genres.First(g => g.Id == gId).Name).ToArray();

        if (config.ExtendedTagging && fileIsTaggable)
        {
            result.Director = episodeResult.Crew.Find(c => c.Job == "Director")?.Name;

            var credits = await tmdb.GetTvEpisodeCreditsAsync(showData.Id, lookupSeason, lookupEpisode);
            result.Actors = credits.Cast.Select(c => c.Name).ToArray();
            result.Characters = credits.Cast.Select(c => c.Character).ToArray();
        }

        return FindResult.Success;
    }

    private async Task FindPosterAsync(TVFileMetadata result, IUserInterface ui)
    {
        if (CachedSeasonPosters.TryGetValue((result.SeriesName, result.Season), out var url))
        {
            result.CoverURL = url;
        }
        else
        {
            var seriesImages = await tmdb.GetTvShowImagesAsync(result.Id, $"{config.Language},null");

            if (seriesImages.Posters.Count > 0)
            {
                var bestVotedImage = seriesImages.Posters.OrderByDescending(p => p.VoteAverage).First();

                result.CoverURL = $"https://image.tmdb.org/t/p/original/{bestVotedImage.FilePath}";
                CachedSeasonPosters.Add((result.SeriesName, result.Season), result.CoverURL);
            }
            else
            {
                ui.SetStatus("Error: Failed to find episode cover", MessageType.Error);
                result.Complete = false;
            }
        }
    }
    
    private static double SeriesNameSimilarity(string parsedName, string seriesName)
    {
        if (seriesName.ToLower().Contains(parsedName.ToLower()))
        {
            return parsedName.Length / (double) seriesName.Length;
        }

        return 0;
    }
}