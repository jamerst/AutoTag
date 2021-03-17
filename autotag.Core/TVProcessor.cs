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
        private TMDbClient tmdb;
        private Dictionary<string, List<SearchTv>> seriesCache =
            new Dictionary<string, List<SearchTv>>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<(string, int), string> seasonPosterCache =
            new Dictionary<(string, int), string>();

        public TVProcessor(string apiKey) {
            this.tmdb = new TMDbClient(apiKey);
        }

        public async Task<bool> process(
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
            if (!seriesCache.ContainsKey(episodeData.SeriesName)) { // if not already searched for series
                SearchContainer<SearchTv> searchResults = await tmdb.SearchTvShowAsync(episodeData.SeriesName);

                List<SearchTv> seriesResults = searchResults.Results
                    .OrderByDescending(result => SeriesNameSimilarity(episodeData.SeriesName, result.Name))
                    .ToList();

                if (config.manualMode) {
                    int chosen = selectResult(seriesResults
                        .Select(t => (t.Name, t.FirstAirDate?.Year.ToString() ?? "Unknown")).ToList());

                    seriesCache.Add(episodeData.SeriesName, new List<SearchTv> { seriesResults[chosen] });
                } else if (seriesResults.Count == 0) {
                    setStatus($"Error: Cannot find series {episodeData.SeriesName} on TheMovieDB", MessageType.Error);
                    result.Success = false;
                    return false;
                } else {
                    seriesCache.Add(episodeData.SeriesName, seriesResults);
                }
            }

            // try searching for each series search result
            foreach (var series in seriesCache[episodeData.SeriesName]) {
                result.Id = series.Id;
                result.SeriesName = series.Name;

                TvEpisode episodeResult = await tmdb.GetTvEpisodeAsync(series.Id, episodeData.Season, episodeData.Episode);

                if (episodeResult == null) {
                    if (series.Id == seriesCache[episodeData.SeriesName].Last().Id) {
                        setStatus($"Error: Cannot find {episodeData} on TheMovieDB", MessageType.Error);

                        return false;
                    }
                    continue;
                }

                result.Title = episodeResult.Name;
                result.Overview = episodeResult.Overview;
                break;
            }

            setStatus($"Found {episodeData} ({result.Title}) on TheMovieDB", MessageType.Information);

            if (config.addCoverArt) {
                if (seasonPosterCache.TryGetValue((result.SeriesName, result.Season), out string url)) {
                    result.CoverURL = url;
                } else {
                    PosterImages images = await tmdb.GetTvSeasonImagesAsync(result.Id, result.Season, $"{CultureInfo.CurrentCulture.TwoLetterISOLanguageName},null");

                    if (images.Posters.Count > 0) {
                        images.Posters.OrderByDescending(p => p.VoteAverage);

                        result.CoverURL = $"https://image.tmdb.org/t/p/original/{images.Posters[0].FilePath}";
                        seasonPosterCache.Add((result.SeriesName, result.Season), result.CoverURL);
                    } else {
                        ImagesWithId seriesImages = await tmdb.GetTvShowImagesAsync(result.Id, $"{CultureInfo.CurrentCulture.TwoLetterISOLanguageName},null");

                        if (seriesImages.Posters.Count > 0) {
                            seriesImages.Posters.OrderByDescending(p => p.VoteAverage);

                            result.CoverURL = $"https://image.tmdb.org/t/p/original/{images.Posters[0].FilePath}";
                            seasonPosterCache.Add((result.SeriesName, result.Season), result.CoverURL);
                        } else {
                            setStatus($"Error: Failed to find episode cover", MessageType.Error);
                            result.Complete = false;
                        }
                    }
                }
            }
            #endregion

            result.CoverFilename = result.CoverURL?.Split('/').Last() ?? "";

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
