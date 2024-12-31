using System.Text.RegularExpressions;
using AutoTag.Core.Files;
using AutoTag.Core.TMDB;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;

namespace AutoTag.Core.TV;
public class TVProcessor(ITMDBService tmdb, IFileWriter writer, ITVCache cache, IUserInterface ui, AutoTagConfig config) : IProcessor
{
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
                ui.SetStatus("File skipped", MessageType.Warning);
                return true;
        }
        
        var result = new TVFileMetadata
        {
            Season = fileNameData.Season,
            Episode = fileNameData.Episode
        };
        
        // try searching for each series search result
        foreach (var show in cache.GetShow(fileNameData.SeriesName))
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
            await FindPosterAsync(result);
        }

        var taggingSuccess = await writer.WriteAsync(file, result);

        return taggingSuccess && result.Success && result.Complete;
    }

    public TVFileMetadata? ParseFileName(TaggingFile file)
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
                    ui.SetStatus("Error: Unable to parse required information from filename", MessageType.Error);
                }

                return null;
            }
        }

        return episodeData;
    }

    public async Task<FindResult> FindShowAsync(string seriesName)
    {
        if (cache.ShowIsCached(seriesName))
        {
            return FindResult.Success;
        }
        
        // if not already searched for series
        var searchResults = await tmdb.SearchTvShowAsync(seriesName);

        var seriesResults = searchResults.Results
            .OrderByDescending(searchResult => SeriesNameSimilarity(seriesName, searchResult.Name))
            .ToList();

        if (seriesResults.Count == 0)
        {
            ui.SetStatus($"Error: Cannot find series {seriesName} on TheMovieDB", MessageType.Error);
            return FindResult.Fail;
        }

        List<ShowResults> resultsToCache;
        if (config.ManualMode)
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
                resultsToCache = [chosenSeries];
                ui.SetStatus($"Selected {chosenSeries.Name} ({chosenSeries.FirstAirDate?.Year.ToString() ?? "Unknown"})", MessageType.Information);
            }
            else
            {
                return FindResult.Skip;
            }
        }
        else
        {
            resultsToCache = ShowResults.FromSearchResults(seriesResults);
        }

        if (config.EpisodeGroup)
        {
            var (result, newShow) = await FindEpisodeGroupAsync(resultsToCache);

            if (result.HasValue)
            {
                return result.Value;
            }

            if (newShow != null)
            {
                resultsToCache = [newShow];
            }
        }
        
        cache.AddShow(seriesName, resultsToCache);
        return FindResult.Success;
    }

    public async Task<(FindResult?, ShowResults?)> FindEpisodeGroupAsync(List<ShowResults> searchResults)
    {
        for (int i = 0; i < searchResults.Count; i++)
        {
            var seriesResult = searchResults[i];
            var tvShow = await tmdb.GetTvShowWithEpisodeGroupsAsync(seriesResult.TvSearchResult.Id);
            var groups = tvShow.EpisodeGroups;

            if (groups.Results.Count != 0)
            {
                var options = groups.Results
                    .Select(g => $"[{g.Type}] {g.Name} ({g.GroupCount} seasons, {g.EpisodeCount} episodes)")
                    .ToList();

                if (searchResults.Count > 1 && i < searchResults.Count - 1)
                {
                    options.Add("(Skip to next search result)");
                }
                
                var chosenGroup = ui.SelectOption(
                    $"Please choose an episode ordering for {seriesResult.TvSearchResult.Name}:",
                    options
                );

                if (chosenGroup.HasValue)
                {
                    if (chosenGroup > groups.Results.Count - 1)
                    {
                        // skip to next search result option was selected
                        continue;
                    }
                    
                    var groupInfo = await tmdb.GetTvEpisodeGroupsAsync(groups.Results[chosenGroup.Value].Id);
                    if (groupInfo is null)
                    {
                        ui.SetStatus($@"Error: Could not retrieve TV episode group for show ""{tvShow.Name}""",
                            MessageType.Error);
                        return (FindResult.Fail, null);
                    }
                    else if (seriesResult.AddEpisodeGroup(groupInfo, out string? failureReason))
                    {
                        ui.SetStatus($"Selected {groupInfo.Name} episode ordering", MessageType.Information);
                        
                        // override cache to force the show for the selected episode group when there were
                        // multiple search results
                        return (null, seriesResult);
                    }
                    else
                    {
                        ui.SetStatus($@"Error: Cannot process episode group ""{groupInfo.Name}"" ({failureReason})", MessageType.Error);
                        return (FindResult.Fail, null);
                    }
                }
                else
                {
                    return (FindResult.Skip, null);
                }
            }
            else
            {
                ui.SetStatus($@"No episode groups found for show ""{tvShow.Name}"".", MessageType.Warning | MessageType.Log);
            }
        }
        
        ui.SetStatus("No episode groups found", MessageType.Warning);

        return (null, null);
    }

    private async Task<FindResult> FindEpisodeAsync(TVFileMetadata fileNameData, ShowResults show, TVFileMetadata result, bool fileIsTaggable)
    {
        var showData = show.TvSearchResult;
        
        var lookupSeason = fileNameData.Season;
        var lookupEpisode = fileNameData.Episode;

        if (show.HasEpisodeGroupMapping)
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

        if (!cache.TryGetSeason(showData.Id, lookupSeason, out var seasonResult))
        {
            seasonResult = await tmdb.GetTvSeasonAsync(showData.Id, lookupSeason);

            if (seasonResult == null)
            {
                if (showData.Id == cache.GetShow(fileNameData.SeriesName)[^1].TvSearchResult.Id)
                {
                    ui.SetStatus($"Error: Cannot find {fileNameData} on TheMovieDB", MessageType.Error);
                    return FindResult.Fail;
                }
                
                return FindResult.Skip;
            }
            
            cache.AddSeason(showData.Id, lookupSeason, seasonResult);
        }
        
        result.SeasonEpisodes = seasonResult.Episodes.Count;

        if (!string.IsNullOrEmpty(seasonResult.PosterPath))
        {
            result.CoverURL = $"https://image.tmdb.org/t/p/original/{seasonResult.PosterPath}";
        }

        var episodeResult = seasonResult.Episodes.Find(e => e.EpisodeNumber == lookupEpisode);
        if (episodeResult == default)
        {
            if (showData.Id == cache.GetShow(fileNameData.SeriesName)[^1].TvSearchResult.Id)
            {
                ui.SetStatus($"Error: Cannot find {fileNameData} on TheMovieDB", MessageType.Error);

                return FindResult.Fail;
            }
            
            return FindResult.Skip;
        }

        result.Title = episodeResult.Name;
        result.Overview = episodeResult.Overview;
        
        result.Genres = await tmdb.GetTvGenreNamesAsync(show.TvSearchResult.GenreIds);

        if (config.ExtendedTagging && fileIsTaggable)
        {
            result.Director = episodeResult.Crew.Find(c => c.Job == "Director")?.Name;

            var credits = await tmdb.GetTvEpisodeCreditsAsync(showData.Id, lookupSeason, lookupEpisode);
            result.Actors = credits.Cast.Select(c => c.Name).ToArray();
            result.Characters = credits.Cast.Select(c => c.Character).ToArray();
        }

        return FindResult.Success;
    }

    private async Task FindPosterAsync(TVFileMetadata result)
    {
        if (cache.TryGetSeasonPoster(result.Id, result.Season, out var url))
        {
            result.CoverURL = url;
        }
        else
        {
            var seriesImages = await tmdb.GetTvShowImagesAsync(result.Id);

            if (seriesImages.Posters.Count > 0)
            {
                var bestVotedImage = seriesImages.Posters.OrderByDescending(p => p.VoteAverage).First();

                result.CoverURL = $"https://image.tmdb.org/t/p/original/{bestVotedImage.FilePath}";
                cache.AddSeasonPoster(result.Id, result.Season, result.CoverURL);
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