﻿using System.Text.RegularExpressions;

using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;

namespace AutoTag.Core.TV;
public class TVProcessor : IProcessor
{
    private readonly TMDbClient _tmdb;
    private readonly Dictionary<string, List<ShowResults>> _shows = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(int, int), TvSeason> _seasons = new();
    private readonly Dictionary<(string, int), string> _seasonPosters = new();
    private IEnumerable<Genre> _genres = [];

    public TVProcessor(string apiKey, AutoTagConfig config)
    {
        _tmdb = new TMDbClient(apiKey);
        _tmdb.DefaultLanguage = config.Language;
        _tmdb.DefaultImageLanguage = config.Language;
    }

    public async Task<bool> ProcessAsync(
        TaggingFile file,
        Action<string> setPath,
        Action<string, MessageType> setStatus,
        Func<List<(string, string)>, int?> selectResult,
        AutoTagConfig config,
        FileWriter writer
    )
    {
        var result = new TVFileMetadata();

        #region Filename parsing
        TVFileMetadata episodeData;

        if (string.IsNullOrEmpty(config.ParsePattern))
        {
            try
            {
                episodeData = EpisodeParser.ParseEpisodeInfo(Path.GetFileName(file.Path)); // Parse info from filename
            }
            catch (FormatException ex)
            {
                setStatus($"Error: {ex.Message}", MessageType.Error);
                return false;
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
                    setStatus($"Error: Unable to parse required information from filename ({ex.GetType().Name}: {ex.Message})", MessageType.Error);
                }
                else
                {
                    setStatus($"Error: Unable to parse required information from filename", MessageType.Error);
                }
                return false;
            }
        }

        result.Season = episodeData.Season;
        result.Episode = episodeData.Episode;

        if (config.Verbose)
        {
            setStatus($"Parsed file as {episodeData}", MessageType.Information);
        }
        #endregion

        #region TMDB API searching
        if (!_shows.ContainsKey(episodeData.SeriesName))
        {
            // if not already searched for series
            SearchContainer<SearchTv> searchResults = await _tmdb.SearchTvShowAsync(episodeData.SeriesName);

            var seriesResults = searchResults.Results
                .OrderByDescending(searchResult => SeriesNameSimilarity(episodeData.SeriesName, searchResult.Name))
                .ToList();

            // using episode groups, requires the manual selection of a show
            if (config.ManualMode || config.EpisodeGroup)
            {
                var chosen = selectResult(seriesResults
                    .Select(t => (t.Name, t.FirstAirDate?.Year.ToString() ?? "Unknown"))
                    .ToList()
                );

                if (chosen.HasValue)
                {
                    _shows.Add(episodeData.SeriesName, [seriesResults[chosen.Value]]);
                }
                else
                {
                    setStatus("File skipped", MessageType.Warning);
                    return true;
                }
            }
            else if (seriesResults.Count == 0)
            {
                setStatus($"Error: Cannot find series {episodeData.SeriesName} on TheMovieDB", MessageType.Error);
                result.Success = false;
                return false;
            }
            else
            {
                _shows.Add(episodeData.SeriesName, ShowResults.FromSearchResults(seriesResults));
            }

            if (config.EpisodeGroup)
            {
                var seriesResult = _shows[episodeData.SeriesName][0];
                var tvShow = await _tmdb.GetTvShowAsync(seriesResult.TvSearchResult.Id, TvShowMethods.EpisodeGroups);
                var groups = tvShow.EpisodeGroups;

                if (groups.Results.Count != 0)
                {
                    var chosenGroup = selectResult(groups.Results.Select(group =>
                        ($"[{group.Type}] {group.Name}", $"{group.GroupCount} seasons, {group.EpisodeCount} episodes")).ToList());

                    if (chosenGroup.HasValue)
                    {
                        var groupInfo = await _tmdb.GetTvEpisodeGroupsAsync(groups.Results[chosenGroup.Value].Id, config.Language);
                        if (groupInfo is null)
                        {
                            setStatus($"Error: Could not retrieve TV episode groups for show \"{tvShow.Name}\"", MessageType.Error);
                            result.Success = false;
                            return false;
                        }
                        
                        if (!seriesResult.AddEpisodeGroup(groupInfo))
                        {
                            setStatus($"Error: Unable to generate a unique season-episode mapping for collection group \"{groupInfo.Name}\"", MessageType.Error);
                            result.Success = false;
                            return false;
                        }
                    }
                    else
                    {
                        setStatus("File skipped", MessageType.Warning);
                        return true;
                    }
                }
                else
                {
                    setStatus($"No episode groups found for series", MessageType.Warning);
                }
            }
        }

        // try searching for each series search result
        foreach (var show in _shows[episodeData.SeriesName])
        {
            var showData = show.TvSearchResult;

            var lookupSeason = episodeData.Season;
            var lookupEpisode = episodeData.Episode;

            if (show.EpisodeGroupMappingTable != null)
            {
                if (!show.TryGetMapping(episodeData.Season, episodeData.Episode, out var groupNumbering))
                {
                    setStatus($"Error: Cannot find {episodeData} in episode group on TheMovieDB", MessageType.Error);
                    return false;
                }
                lookupSeason = groupNumbering!.Value.season;
                lookupEpisode = groupNumbering.Value.episode;
            }

            result.Id = showData.Id;
            result.SeriesName = showData.Name;

            if (!_seasons.TryGetValue((showData.Id, lookupSeason), out var seasonResult))
            {
                seasonResult = await _tmdb.GetTvSeasonAsync(showData.Id, lookupSeason);

                if (seasonResult == null)
                {
                    if (showData.Id == _shows[episodeData.SeriesName][-1].TvSearchResult.Id)
                    {
                        setStatus($"Error: Cannot find {episodeData} on TheMovieDB", MessageType.Error);
                        return false;
                    }
                    continue;
                }
                _seasons.Add((showData.Id, lookupSeason), seasonResult);
            }
            result.SeasonEpisodes = seasonResult.Episodes.Count;

            if (!string.IsNullOrEmpty(seasonResult.PosterPath))
            {
                result.CoverURL = $"https://image.tmdb.org/t/p/original/{seasonResult.PosterPath}";
            }

            var episodeResult = seasonResult.Episodes.Find(e => e.EpisodeNumber == lookupEpisode);
            if (episodeResult == default)
            {
                if (showData.Id == _shows[episodeData.SeriesName][-1].TvSearchResult.Id)
                {
                    setStatus($"Error: Cannot find {episodeData} on TheMovieDB", MessageType.Error);

                    return false;
                }
                continue;
            }

            result.Title = episodeResult.Name;
            result.Overview = episodeResult.Overview;


            if (!_genres.Any())
            {
                _genres = await _tmdb.GetTvGenresAsync();
            }
            result.Genres = showData.GenreIds.Select(gId => _genres.First(g => g.Id == gId).Name).ToArray();

            if (config.ExtendedTagging && file.Taggable)
            {
                result.Director = episodeResult.Crew.Find(c => c.Job == "Director")?.Name;

                var credits = await _tmdb.GetTvEpisodeCreditsAsync(showData.Id, lookupSeason, lookupEpisode);
                result.Actors = credits.Cast.Select(c => c.Name).ToArray();
                result.Characters = credits.Cast.Select(c => c.Character).ToArray();
            }
            break;
        }

        setStatus($"Found {episodeData} ({result.Title}) on TheMovieDB", MessageType.Information);

        if (config.AddCoverArt && string.IsNullOrEmpty(result.CoverURL) && file.Taggable)
        {
            if (_seasonPosters.TryGetValue((result.SeriesName, result.Season), out var url))
            {
                result.CoverURL = url;
            }
            else
            {
                var seriesImages = await _tmdb.GetTvShowImagesAsync(result.Id, $"{config.Language},null");

                if (seriesImages.Posters.Any())
                {
                    var bestVotedImage = seriesImages.Posters.OrderByDescending(p => p.VoteAverage).First();

                    result.CoverURL = $"https://image.tmdb.org/t/p/original/{bestVotedImage.FilePath}";
                    _seasonPosters.Add((result.SeriesName, result.Season), result.CoverURL);
                }
                else
                {
                    setStatus("Error: Failed to find episode cover", MessageType.Error);
                    result.Complete = false;
                }
            }
        }
        #endregion

        result.CoverFilename = result.CoverURL?.Split('/')[^1];

        var taggingSuccess = await writer.WriteAsync(file, result, setPath, setStatus, config);

        return taggingSuccess && result.Success && result.Complete;
    }

    private static double SeriesNameSimilarity(string parsedName, string seriesName)
    {
        if (seriesName.ToLower().Contains(parsedName.ToLower()))
        {
            return (double) parsedName.Length / (double) seriesName.Length;
        }

        return 0;
    }

    public void Dispose()
    {
        _tmdb.Dispose();
    }
}