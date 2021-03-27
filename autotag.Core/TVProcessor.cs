using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;

namespace autotag.Core {
    public class TVProcessor : IProcessor {
        private readonly TMDbClient _tmdb;
        private Dictionary<string, List<SearchTv>> _shows =
            new Dictionary<string, List<SearchTv>>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<(int, int), TvSeason> _seasons =
            new Dictionary<(int, int), TvSeason>();
        private Dictionary<(string, int), string> _seasonPosters =
            new Dictionary<(string, int), string>();
        private List<Genre> Genres = null;

        public TVProcessor(string apiKey) {
            this._tmdb = new TMDbClient(apiKey);
        }

        public async Task<bool> Process(
            string filePath,
            Action<string> setPath,
            Action<string, MessageType> setStatus,
            Func<List<(string, string)>, int> selectResult,
            AutoTagConfig config
        ) {
            FileMetadata result = new FileMetadata(FileMetadata.Types.TV);

            #region Filename parsing
            FileMetadata episodeData;

            if (string.IsNullOrEmpty(config.parsePattern)) {
                try {
                    episodeData = EpisodeParser.ParseEpisodeInfo(Path.GetFileName(filePath)); // Parse info from filename
                } catch (FormatException ex) {
                    setStatus($"Error: {ex.Message}", MessageType.Error);
                    return false;
                }
            } else {
                try {
                    var match = Regex.Match(Path.GetFullPath(filePath), config.parsePattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

                    episodeData = new FileMetadata(FileMetadata.Types.TV);
                    episodeData.SeriesName = match.Groups["SeriesName"].Value;
                    episodeData.Season = int.Parse(match.Groups["Season"].Value);
                    episodeData.Episode = int.Parse(match.Groups["Episode"].Value);
                } catch (FormatException ex) {
                    if (config.verbose) {
                        setStatus($"Error: Unable to parse required information from filename ({ex.GetType().Name}: {ex.Message})", MessageType.Error);
                    } else {
                        setStatus($"Error: Unable to parse required information from filename", MessageType.Error);
                    }
                    return false;
                }
            }

            result.Season = episodeData.Season;
            result.Episode = episodeData.Episode;

            setStatus($"Parsed file as {episodeData}", MessageType.Information);
            #endregion

            #region TMDB API searching
            if (!_shows.ContainsKey(episodeData.SeriesName)) { // if not already searched for series
                SearchContainer<SearchTv> searchResults = await _tmdb.SearchTvShowAsync(episodeData.SeriesName);

                List<SearchTv> seriesResults = searchResults.Results
                    .OrderByDescending(result => SeriesNameSimilarity(episodeData.SeriesName, result.Name))
                    .ToList();

                if (config.manualMode) {
                    int chosen = selectResult(seriesResults
                        .Select(t => (t.Name, t.FirstAirDate?.Year.ToString() ?? "Unknown")).ToList());

                    _shows.Add(episodeData.SeriesName, new List<SearchTv> { seriesResults[chosen] });
                } else if (seriesResults.Count == 0) {
                    setStatus($"Error: Cannot find series {episodeData.SeriesName} on TheMovieDB", MessageType.Error);
                    result.Success = false;
                    return false;
                } else {
                    _shows.Add(episodeData.SeriesName, seriesResults);
                }
            }

            // try searching for each series search result
            foreach (var show in _shows[episodeData.SeriesName]) {
                result.Id = show.Id;
                result.SeriesName = show.Name;

                TvSeason seasonResult;
                if (!_seasons.TryGetValue((show.Id, episodeData.Season), out seasonResult)) {
                    seasonResult = await _tmdb.GetTvSeasonAsync(show.Id, episodeData.Season);

                    if (seasonResult == null) {
                        if (show.Id == _shows[episodeData.SeriesName].Last().Id) {
                            setStatus($"Error: Cannot find {episodeData} on TheMovieDB", MessageType.Error);

                            return false;
                        }
                        continue;
                    } else {
                        _seasons.Add((show.Id, episodeData.Season), seasonResult);
                    }
                }
                result.SeasonEpisodes = seasonResult.Episodes.Count;

                if (!string.IsNullOrEmpty(seasonResult.PosterPath)) {
                    result.CoverURL = $"https://image.tmdb.org/t/p/original/{seasonResult.PosterPath}";
                }

                TvSeasonEpisode episodeResult;
                if ((episodeResult = seasonResult.Episodes.FirstOrDefault(e => e.EpisodeNumber == episodeData.Episode)) == default(TvSeasonEpisode)) {
                    if (show.Id == _shows[episodeData.SeriesName].Last().Id) {
                        setStatus($"Error: Cannot find {episodeData} on TheMovieDB", MessageType.Error);

                        return false;
                    }
                    continue;
                }

                result.Title = episodeResult.Name;
                result.Overview = episodeResult.Overview;


                if (Genres == null) {
                    Genres = await _tmdb.GetTvGenresAsync();
                }
                result.Genres = show.GenreIds.Select(gId => Genres.First(g => g.Id == gId).Name).ToArray();
                
                if (config.extendedTagging) {
                    result.Director = episodeResult.Crew.FirstOrDefault(c => c.Job == "Director")?.Name;

                    var credits = await _tmdb.GetTvEpisodeCreditsAsync(show.Id, result.Season, result.Episode);
                    result.Actors = credits.Cast.Select(c => c.Name).ToArray();
                    result.Characters = credits.Cast.Select(c => c.Character).ToArray();
                }
                break;
            }

            setStatus($"Found {episodeData} ({result.Title}) on TheMovieDB", MessageType.Information);

            if (config.addCoverArt && string.IsNullOrEmpty(result.CoverURL)) {
                if (_seasonPosters.TryGetValue((result.SeriesName, result.Season), out string url)) {
                    result.CoverURL = url;
                } else {
                    ImagesWithId seriesImages = await _tmdb.GetTvShowImagesAsync(result.Id, $"{CultureInfo.CurrentCulture.TwoLetterISOLanguageName},null");

                    if (seriesImages.Posters.Count > 0) {
                        seriesImages.Posters.OrderByDescending(p => p.VoteAverage);

                        result.CoverURL = $"https://image.tmdb.org/t/p/original/{seriesImages.Posters[0].FilePath}";
                        _seasonPosters.Add((result.SeriesName, result.Season), result.CoverURL);
                    } else {
                        setStatus($"Error: Failed to find episode cover", MessageType.Error);
                        result.Complete = false;
                    }
                }
            }
            #endregion

            result.CoverFilename = result.CoverURL?.Split('/').Last();

            bool taggingSuccess = FileWriter.write(filePath, result, setPath, setStatus, config);

            return taggingSuccess && result.Success && result.Complete;
        }

        private double SeriesNameSimilarity(string parsedName, string seriesName) {
            if (seriesName.ToLower().Contains(parsedName.ToLower())) {
                return (double) parsedName.Length / (double) seriesName.Length;
            }

            return 0;
        }
    }
}
