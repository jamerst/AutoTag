using System.Globalization;
using System.Text.RegularExpressions;

using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;

namespace AutoTag.Core.TV;
public class TVProcessor : IProcessor, IDisposable
{
    private readonly TMDbClient _tmdb;
    private Dictionary<string, List<SearchTv>> _shows =
        new Dictionary<string, List<SearchTv>>(StringComparer.OrdinalIgnoreCase);
    private Dictionary<(int, int), TvSeason> _seasons =
        new Dictionary<(int, int), TvSeason>();
    private Dictionary<(string, int), string> _seasonPosters =
        new Dictionary<(string, int), string>();
    private IEnumerable<Genre> Genres = Enumerable.Empty<Genre>();

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
        TVFileMetadata result = new TVFileMetadata();

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

                episodeData = new TVFileMetadata();
                episodeData.SeriesName = match.Groups["SeriesName"].Value;
                episodeData.Season = int.Parse(match.Groups["Season"].Value);
                episodeData.Episode = int.Parse(match.Groups["Episode"].Value);
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

            List<SearchTv> seriesResults = searchResults.Results
                .OrderByDescending(result => SeriesNameSimilarity(episodeData.SeriesName, result.Name))
                .ToList();

            if (config.ManualMode)
            {
                int? chosen = selectResult(seriesResults
                    .Select(t => (t.Name, t.FirstAirDate?.Year.ToString() ?? "Unknown"))
                    .ToList()
                );

                if (chosen.HasValue)
                {
                    _shows.Add(episodeData.SeriesName, new List<SearchTv> { seriesResults[chosen.Value] });
                }
                else
                {
                    setStatus("File skipped", MessageType.Warning);
                    return true;
                }
            }
            else if (!seriesResults.Any())
            {
                setStatus($"Error: Cannot find series {episodeData.SeriesName} on TheMovieDB", MessageType.Error);
                result.Success = false;
                return false;
            }
            else
            {
                _shows.Add(episodeData.SeriesName, seriesResults);
            }
        }

        // try searching for each series search result
        foreach (var show in _shows[episodeData.SeriesName])
        {
            result.Id = show.Id;
            result.SeriesName = show.Name;

            TvSeason? seasonResult;
            if (!_seasons.TryGetValue((show.Id, episodeData.Season), out seasonResult))
            {
                seasonResult = await _tmdb.GetTvSeasonAsync(show.Id, episodeData.Season);

                if (seasonResult == null)
                {
                    if (show.Id == _shows[episodeData.SeriesName].Last().Id)
                    {
                        setStatus($"Error: Cannot find {episodeData} on TheMovieDB", MessageType.Error);

                        return false;
                    }
                    continue;
                }
                else
                {
                    _seasons.Add((show.Id, episodeData.Season), seasonResult);
                }
            }
            result.SeasonEpisodes = seasonResult.Episodes.Count;

            if (!string.IsNullOrEmpty(seasonResult.PosterPath))
            {
                result.CoverURL = $"https://image.tmdb.org/t/p/original/{seasonResult.PosterPath}";
            }

            TvSeasonEpisode? episodeResult = seasonResult.Episodes.FirstOrDefault(e => e.EpisodeNumber == episodeData.Episode);
            if (episodeResult == default)
            {
                if (show.Id == _shows[episodeData.SeriesName].Last().Id)
                {
                    setStatus($"Error: Cannot find {episodeData} on TheMovieDB", MessageType.Error);

                    return false;
                }
                continue;
            }

            result.Title = episodeResult.Name;
            result.Overview = episodeResult.Overview;


            if (!Genres.Any())
            {
                Genres = await _tmdb.GetTvGenresAsync();
            }
            result.Genres = show.GenreIds.Select(gId => Genres.First(g => g.Id == gId).Name).ToArray();

            if (config.ExtendedTagging && file.Taggable)
            {
                result.Director = episodeResult.Crew.FirstOrDefault(c => c.Job == "Director")?.Name;

                var credits = await _tmdb.GetTvEpisodeCreditsAsync(show.Id, result.Season, result.Episode);
                result.Actors = credits.Cast.Select(c => c.Name).ToArray();
                result.Characters = credits.Cast.Select(c => c.Character).ToArray();
            }
            break;
        }

        setStatus($"Found {episodeData} ({result.Title}) on TheMovieDB", MessageType.Information);

        if (config.AddCoverArt && string.IsNullOrEmpty(result.CoverURL) && file.Taggable)
        {
            if (_seasonPosters.TryGetValue((result.SeriesName, result.Season), out string? url))
            {
                result.CoverURL = url;
            }
            else
            {
                ImagesWithId seriesImages = await _tmdb.GetTvShowImagesAsync(result.Id, $"{config.Language},null");

                if (seriesImages.Posters.Any())
                {
                    seriesImages.Posters.OrderByDescending(p => p.VoteAverage);

                    result.CoverURL = $"https://image.tmdb.org/t/p/original/{seriesImages.Posters[0].FilePath}";
                    _seasonPosters.Add((result.SeriesName, result.Season), result.CoverURL);
                }
                else
                {
                    setStatus($"Error: Failed to find episode cover", MessageType.Error);
                    result.Complete = false;
                }
            }
        }
        #endregion

        result.CoverFilename = result.CoverURL?.Split('/').Last();

        bool taggingSuccess = await writer.WriteAsync(file, result, setPath, setStatus, config);

        return taggingSuccess && result.Success && result.Complete;
    }

    private double SeriesNameSimilarity(string parsedName, string seriesName)
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