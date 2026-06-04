using AutoTag.Core.Config;
using AutoTag.Core.Files;
using AutoTag.Core.Files.Parsing;
using AutoTag.Core.TMDB;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;

namespace AutoTag.Core.TV;

public class TVProcessor(ITMDBService tmdb, IFileWriter writer, ITVCache cache, IUserInterface ui, AutoTagConfig config)
    : IProcessor
{
    private readonly Dictionary<int, EpisodeNumberMapping> _episodeNumberMappings = new();

    public async Task<ProcessResult> ProcessAsync(TaggingFile file)
    {
        if (file.TVDetails is null)
        {
            return ProcessResult.ParseFailure;
        }

        ui.SetStatus($"Parsed file as {file.TVDetails}", MessageType.Log);

        var (findShowResult, showResults) = await FindShowAsync(file.TVDetails.SeriesName, file.TVDetails.Year);
        switch (findShowResult)
        {
            case FindResult.Fail:
                return ProcessResult.NotFound;
            case FindResult.Skip:
                ui.SetStatus("File skipped", MessageType.Warning);
                return ProcessResult.Skipped;
        }

        TVFileMetadata? metadata = null;
        string? lastResultMessage = null;

        // try searching for episode in each series search result
        foreach (var show in showResults!)
        {
            var (findEpisodeResult, resultMetadata, resultMessage) =
                await FindEpisodeAsync(file.TVDetails, show, file.Taggable);
            lastResultMessage = resultMessage;

            if (findEpisodeResult == FindResult.Fail)
            {
                return ProcessResult.NotFound;
            }

            if (findEpisodeResult == FindResult.Success)
            {
                metadata = resultMetadata;
                lastResultMessage = null;
                break;
            }
        }

        // if reached the end of the search results without finding the episode
        if (metadata is null)
        {
            if (lastResultMessage is not null)
            {
                ui.SetStatus(lastResultMessage, MessageType.Error);
            }

            return ProcessResult.NotFound;
        }

        ui.SetStatus($"Found {metadata} on TheMovieDB", MessageType.Information);

        if (config.AddCoverArt && string.IsNullOrEmpty(metadata.CoverURL) && file.Taggable)
        {
            await FindPosterAsync(metadata);
        }

        var taggingSuccess = await writer.WriteAsync(file, metadata);

        return taggingSuccess && metadata.Complete
            ? ProcessResult.Success
            : ProcessResult.Fail;
    }

    public async Task<(FindResult Result, List<ShowResults>? Shows)> FindShowAsync(string seriesName, int? year)
    {
        if (cache.TryGetShow(seriesName, year, out var cachedResult))
        {
            return (FindResult.Success, cachedResult);
        }

        // if not already searched for series
        var searchResults = await tmdb.SearchTvShowAsync(seriesName);

        var seriesResults = searchResults.Results!
            .OrderByDescending(searchResult => SeriesNameSimilarity(seriesName, searchResult.Name!))
            .ThenByDescending(r => r.FirstAirDate.HasValue && r.FirstAirDate.Value.Year == year)
            .ToList();

        if (seriesResults.Count == 0)
        {
            ui.SetStatus($"Error: Cannot find series {seriesName} on TheMovieDB", MessageType.Error);
            return (FindResult.Fail, null);
        }

        List<ShowResults> resultsToCache;
        if (config.ManualMode)
        {
            var chosen = ui.SelectOption(
                "Please choose an option:",
                seriesResults
                    .Select(t => $"{t.Name} ({t.FirstAirDate?.Year.ToString() ?? "Unknown year"})")
                    .ToList()
            );

            if (chosen.HasValue)
            {
                var chosenSeries = seriesResults[chosen.Value];
                resultsToCache = [chosenSeries];
                ui.SetStatus(
                    $"Selected {chosenSeries.Name} ({chosenSeries.FirstAirDate?.Year.ToString() ?? "Unknown year"})",
                    MessageType.Information);
            }
            else
            {
                return (FindResult.Skip, null);
            }
        }
        else
        {
            resultsToCache = ShowResults.FromSearchResults(seriesResults);
        }

        var result = FindResult.Success;
        if (config.EpisodeGroup)
        {
            var (groupResult, newShow) = await FindEpisodeGroupAsync(resultsToCache);

            if (groupResult.HasValue)
            {
                result = groupResult.Value;
            }

            if (newShow != null)
            {
                resultsToCache = [newShow];
            }
        }

        cache.AddShow(seriesName, year, resultsToCache);
        return (result, resultsToCache);
    }

    public async Task<(FindResult?, ShowResults?)> FindEpisodeGroupAsync(List<ShowResults> searchResults)
    {
        for (var i = 0; i < searchResults.Count; i++)
        {
            var seriesResult = searchResults[i];
            var tvShow = await tmdb.GetTvShowWithEpisodeGroupsAsync(seriesResult.TvSearchResult.Id);
            var groups = tvShow.EpisodeGroups;

            if (groups!.Results!.Count != 0)
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

                    var groupInfo = await tmdb.GetTvEpisodeGroupsAsync(groups.Results[chosenGroup.Value].Id!);
                    if (groupInfo is null)
                    {
                        ui.SetStatus($@"Error: Could not retrieve TV episode group for show ""{tvShow.Name}""",
                            MessageType.Error);
                        return (FindResult.Fail, null);
                    }

                    if (seriesResult.AddEpisodeGroup(groupInfo, out var failureReason))
                    {
                        ui.SetStatus($"Selected {groupInfo.Name} episode ordering", MessageType.Information);

                        // override cache to force the show for the selected episode group when there were
                        // multiple search results
                        return (null, seriesResult);
                    }

                    ui.SetStatus($@"Error: Cannot process episode group ""{groupInfo.Name}"" ({failureReason})",
                        MessageType.Error);
                    return (FindResult.Fail, null);
                }

                return (FindResult.Skip, null);
            }

            ui.SetStatus($@"No episode groups found for show ""{tvShow.Name}""", MessageType.Warning | MessageType.Log);
        }

        ui.SetStatus("No episode groups found", MessageType.Warning);

        return (null, null);
    }

    public async Task<(FindResult Result, TVFileMetadata? metadata, string? LastResultErrorMessage)> FindEpisodeAsync(
        ParsedTVFileName parsedDetails,
        ShowResults show, bool fileIsTaggable)
    {
        var showData = show.TvSearchResult;

        // lookup season/episode is the episode number in the default ordering
        // if episode groups are used we need to map from the ordering scheme used in the file name to the default
        // ordering to find the episode details
        var lookupSeason = parsedDetails.Season;
        var lookupEpisode = parsedDetails.Episode;

        if (show.HasEpisodeGroupMapping)
        {
            if (!parsedDetails.Season.HasValue)
            {
                ui.SetStatus("Error: Cannot apply episode group numbering to absolute episode numbers",
                    MessageType.Error);
                return (FindResult.Fail, null, null);
            }

            if (show.TryGetMapping(parsedDetails.Season.Value, parsedDetails.Episode, out var groupNumbering))
            {
                lookupSeason = groupNumbering.Value.Season;
                lookupEpisode = groupNumbering.Value.Episode;
            }
            else
            {
                ui.SetStatus($"Error: Cannot find {parsedDetails} in episode group on TheMovieDB", MessageType.Error);
                return (FindResult.Fail, null, null);
            }
        }

        var result = await GetEpisodeAsync(showData.Id, lookupSeason, lookupEpisode);
        if (result is null)
        {
            return (FindResult.Skip, null, $"Error: Cannot find {parsedDetails} on TheMovieDB");
        }

        var metadata = new TVFileMetadata
        {
            Id = showData.Id,
            SeriesName = showData.Name!,
            Year = parsedDetails.Year,
            Season = parsedDetails.Season ?? result.Value.Season.SeasonNumber,
            Episode = parsedDetails.Season.HasValue
                ? parsedDetails.Episode
                : (int)result.Value.Episode.EpisodeNumber,
            EndEpisode = parsedDetails.EndEpisode,
            SeasonEpisodes = result.Value.Season.Episodes!.Count,
            CoverURL = !string.IsNullOrEmpty(result.Value.Season.PosterPath)
                ? $"https://image.tmdb.org/t/p/original/{result.Value.Season.PosterPath}"
                : null,
            Title = result.Value.Episode.Name!,
            Overview = result.Value.Episode.Overview,
            Genres = await tmdb.GetTvGenreNamesAsync(show.TvSearchResult.GenreIds!),
            Part = parsedDetails.Part
        };

        if (config.ExtendedTagging && fileIsTaggable)
        {
            metadata.Director = result.Value.Episode.Crew!.Find(c => c.Job == "Director")?.Name;

            var credits = await tmdb.GetTvEpisodeCreditsAsync(showData.Id, result.Value.Season.SeasonNumber,
                (int)result.Value.Episode.EpisodeNumber);
            metadata.Actors = credits.Cast!.Select(c => c.Name!).ToArray();
            metadata.Characters = credits.Cast!.Select(c => c.Character!).ToArray();
        }

        return (FindResult.Success, metadata, null);
    }

    private async Task<TvSeason?> GetSeasonAsync(int showId, int seasonNumber)
    {
        if (cache.TryGetSeason(showId, seasonNumber, out var seasonResult))
        {
            return seasonResult;
        }

        seasonResult = await tmdb.GetTvSeasonAsync(showId, seasonNumber);
        if (seasonResult != null)
        {
            cache.AddSeason(showId, seasonNumber, seasonResult);
        }

        return seasonResult;
    }

    private async Task<(TvSeason Season, TvSeasonEpisode Episode)?> GetEpisodeAsync(int showId, int? seasonNumber,
        int episodeNumber)
    {
        if (seasonNumber.HasValue)
        {
            var season = await GetSeasonAsync(showId, seasonNumber.Value);

            if (season != null && season.Episodes!.TryFind(e => e.EpisodeNumber == episodeNumber, out var episode))
            {
                return (season, episode);
            }

            return null;
        }

        var mapping = await GetEpisodeNumberMapping(showId);
        return mapping.GetByEpisodeNumber(episodeNumber);
    }

    private async Task<EpisodeNumberMapping> GetEpisodeNumberMapping(int showId)
    {
        if (_episodeNumberMappings.TryGetValue(showId, out var mapping))
        {
            return mapping;
        }

        var show = await tmdb.GetTvShowAsync(showId);
        List<TvSeason> seasons = new(show.NumberOfSeasons);
        for (var season = 1; season <= show.NumberOfSeasons; season++)
        {
            var seasonResult = await GetSeasonAsync(showId, season);

            if (seasonResult is not null)
            {
                seasons.Add(seasonResult);
            }
        }

        var newMapping = new EpisodeNumberMapping(seasons);
        _episodeNumberMappings[showId] = newMapping;

        return newMapping;
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

            if (seriesImages.Posters?.Count > 0)
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
        if (seriesName.Contains(parsedName, StringComparison.OrdinalIgnoreCase))
        {
            return parsedName.Length / (double)seriesName.Length;
        }

        return 0;
    }
}