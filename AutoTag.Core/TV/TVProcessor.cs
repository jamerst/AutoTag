using System.Text.RegularExpressions;
using AutoTag.Core.Config;
using AutoTag.Core.Files;
using AutoTag.Core.TMDB;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;

namespace AutoTag.Core.TV;
public class TVProcessor(ITMDBService tmdb, IFileWriter writer, ITVCache cache, IUserInterface ui, AutoTagConfig config) : IProcessor
{
    public async Task<bool> ProcessAsync(TaggingFile file)
    {
        var metadata = ParseFileName(file);
        if (metadata == null)
        {
            return false;
        }

        ui.SetStatus($"Parsed file as {metadata}", MessageType.Log);

        var findShowResult = await FindShowAsync(metadata.SeriesName);
        switch (findShowResult)
        {
            case FindResult.Fail:
                return false;
            case FindResult.Skip:
                ui.SetStatus("File skipped", MessageType.Warning);
                return true;
        }

        string? lastResultMessage = null;
        
        // try searching for episode in each series search result
        foreach (var show in cache.GetShow(metadata.SeriesName))
        {
            var (findEpisodeResult, _lastResultMessage) = await FindEpisodeAsync(metadata, show, file.Taggable);
            lastResultMessage = _lastResultMessage;
            
            if (findEpisodeResult == FindResult.Fail)
            {
                return false;
            }
            else if (findEpisodeResult == FindResult.Success)
            {
                lastResultMessage = null;
                break;
            }
        }

        // if reached the end of the search results without finding the episode
        if (lastResultMessage != null)
        {
            ui.SetStatus(lastResultMessage, MessageType.Error);

            return false;
        }

        ui.SetStatus($"Found {metadata} ({metadata.Title}) on TheMovieDB", MessageType.Information);

        if (config.AddCoverArt && string.IsNullOrEmpty(metadata.CoverURL) && file.Taggable)
        {
            await FindPosterAsync(metadata);
        }

        var taggingSuccess = await writer.WriteAsync(file, metadata);

        return taggingSuccess && metadata.Success && metadata.Complete;
    }

    public TVFileMetadata? ParseFileName(TaggingFile file)
    {
        if (string.IsNullOrEmpty(config.ParsePattern))
        {
            if (EpisodeParser.TryParseEpisodeInfo(Path.GetFileName(file.Path), out var parsedMetadata,
                    out string? failureReason))
            {
                return parsedMetadata;
            }
            else
            {
                ui.SetStatus($"Error: {failureReason}", MessageType.Error);
                return null;
            }
        }
        else
        {
            try
            {
                var match = Regex.Match(Path.GetFullPath(file.Path), config.ParsePattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

                return new TVFileMetadata
                {
                    SeriesName = match.Groups["SeriesName"].Value,
                    Season = int.Parse(match.Groups["Season"].Value),
                    Episode = int.Parse(match.Groups["Episode"].Value)
                };
            }
            catch (FormatException ex)
            {
                ui.SetStatus("Error: Unable to parse required information from filename", MessageType.Error, ex);
                return null;
            }
        }
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
                    .Select(g => $"[[{g.Type}]] {g.Name} ({g.GroupCount} seasons, {g.EpisodeCount} episodes)")
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
                ui.SetStatus($@"No episode groups found for show ""{tvShow.Name}""", MessageType.Warning | MessageType.Log);
            }
        }
        
        ui.SetStatus("No episode groups found", MessageType.Warning);

        return (null, null);
    }

    public async Task<(FindResult Result, string? LastResultErrorMessage)> FindEpisodeAsync(TVFileMetadata metadata, ShowResults show, bool fileIsTaggable)
    {
        var showData = show.TvSearchResult;
        
        // lookup season/episode is the episode number in the default ordering
        // if episode groups are used we need to map from the ordering scheme used in the file name to the default
        // ordering to find the episode details
        var lookupSeason = metadata.Season;
        var lookupEpisode = metadata.Episode;

        if (show.HasEpisodeGroupMapping)
        {
            if (show.TryGetMapping(metadata.Season, metadata.Episode, out var groupNumbering))
            {
                lookupSeason = groupNumbering.Value.Season;
                lookupEpisode = groupNumbering.Value.Episode;
            }
            else
            {
                ui.SetStatus($"Error: Cannot find {metadata} in episode group on TheMovieDB", MessageType.Error);
                return (FindResult.Fail, null);
            }
        }

        metadata.Id = showData.Id;
        metadata.SeriesName = showData.Name;

        if (!cache.TryGetSeason(showData.Id, lookupSeason, out var seasonResult))
        {
            seasonResult = await tmdb.GetTvSeasonAsync(showData.Id, lookupSeason);

            if (seasonResult != null)
            {
                cache.AddSeason(showData.Id, lookupSeason, seasonResult);
            }
        }

        if (seasonResult == null ||
            !seasonResult.Episodes.TryFind(e => e.EpisodeNumber == lookupEpisode, out var episodeResult))
        {
            return (FindResult.Skip, $"Error: Cannot find {metadata} on TheMovieDB");
        }
        
        metadata.SeasonEpisodes = seasonResult.Episodes.Count;

        if (!string.IsNullOrEmpty(seasonResult.PosterPath))
        {
            metadata.CoverURL = $"https://image.tmdb.org/t/p/original/{seasonResult.PosterPath}";
        }

        metadata.Title = episodeResult.Name;
        metadata.Overview = episodeResult.Overview;
        
        metadata.Genres = await tmdb.GetTvGenreNamesAsync(show.TvSearchResult.GenreIds);

        if (config.ExtendedTagging && fileIsTaggable)
        {
            metadata.Director = episodeResult.Crew.Find(c => c.Job == "Director")?.Name;

            var credits = await tmdb.GetTvEpisodeCreditsAsync(showData.Id, lookupSeason, lookupEpisode);
            metadata.Actors = credits.Cast.Select(c => c.Name).ToArray();
            metadata.Characters = credits.Cast.Select(c => c.Character).ToArray();
        }

        return (FindResult.Success, null);
    }

    public async Task FindPosterAsync(TVFileMetadata metadata)
    {
        if (cache.TryGetSeasonPoster(metadata.Id, metadata.Season, out var url))
        {
            metadata.CoverURL = url;
        }
        else
        {
            var seriesImages = await tmdb.GetTvShowImagesAsync(metadata.Id);

            if (seriesImages.Posters.Count > 0)
            {
                var bestVotedImage = seriesImages.Posters.OrderByDescending(p => p.VoteAverage).First();

                metadata.CoverURL = $"https://image.tmdb.org/t/p/original/{bestVotedImage.FilePath}";
                cache.AddSeasonPoster(metadata.Id, metadata.Season, metadata.CoverURL);
            }
            else
            {
                ui.SetStatus("Error: Failed to find episode cover", MessageType.Error);
                metadata.Complete = false;
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