using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using TvDbSharper;
using TvDbSharper.Dto;

namespace autotag.Core {
    public class TVProcessor : IProcessor {

        private ITvDbClient tvdb;
        private Dictionary<string, List<SeriesSearchResult>> seriesResultCache =
            new Dictionary<string, List<SeriesSearchResult>>(StringComparer.OrdinalIgnoreCase);
        private string apiKey;

        public TVProcessor(string apiKey) {
            this.tvdb = new TvDbClient();
            this.apiKey = apiKey;
        }

        public async Task<bool> process(
                string filePath,
                Action<string> setPath,
                Action<string, bool> setStatus,
                Func<List<Tuple<string, string>>, int> selectResult,
                AutoTagConfig config) {

            if (tvdb.Authentication.Token == null) {
                await tvdb.Authentication.AuthenticateAsync(apiKey);
            }

            FileMetadata result = new FileMetadata(FileMetadata.Types.TV);

            #region Filename parsing
            FileMetadata episodeData;

            if (string.IsNullOrEmpty(config.parsePattern)) {
                try {
                    episodeData = EpisodeParser.ParseEpisodeInfo(Path.GetFileName(filePath)); // Parse info from filename
                } catch (FormatException ex) {
                    setStatus($"Error: {ex.Message}", true);
                    return false;
                }
            } else {
                try {
                    var match = Regex.Match(filePath, config.parsePattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

                    episodeData = new FileMetadata(FileMetadata.Types.TV);
                    episodeData.SeriesName = match.Groups["SeriesName"].Value;
                    episodeData.Season = int.Parse(match.Groups["Season"].Value);
                    episodeData.Episode = int.Parse(match.Groups["Episode"].Value);
                } catch (FormatException ex) {
                    if (config.verbose) {
                        setStatus($"Error: {ex.Message}", true);
                    } else {
                        setStatus($"Error: Unable to parse required information from filename", true);
                    }
                    return false;
                }
            }

            result.Season = episodeData.Season;
            result.Episode = episodeData.Episode;

            setStatus($"Parsed file as {episodeData}", false);
            #endregion

            #region TVDB API searching
            if (!seriesResultCache.ContainsKey(episodeData.SeriesName)) { // if not already searched for series
                TvDbResponse<SeriesSearchResult[]> seriesIdResponse;
                try {
                    seriesIdResponse = await tvdb.Search.SearchSeriesByNameAsync(episodeData.SeriesName);
                } catch (TvDbServerException ex) {
                    if (config.verbose) {
                        setStatus($"Error: Cannot find series {episodeData.SeriesName} ({ex.Message})", true);
                    } else {
                        setStatus($"Error: Cannot find series {episodeData.SeriesName} on TheTVDB", true);
                    }
                    return false;
                }

                // sort results by similarity to parsed series name
                List<SeriesSearchResult> seriesResults = seriesIdResponse.Data
                    .OrderByDescending(seriesResult => SeriesNameSimilarity(episodeData.SeriesName, seriesResult.SeriesName))
                    .ToList();

                if (config.manualMode && seriesResults.Count > 1) {
                    int chosen = selectResult(seriesResults.Select(
                        r => new Tuple<string, string>(r.SeriesName, r.FirstAired ?? "Unknown")
                        ).ToList()
                    );

                    // add only the chosen series to cache if in manual mode
                    seriesResultCache.Add(episodeData.SeriesName, new List<SeriesSearchResult> { seriesResults[chosen] });
                } else {
                    seriesResultCache.Add(episodeData.SeriesName, seriesResults);
                }
            }

            // try searching for each series search result
            foreach (var series in seriesResultCache[episodeData.SeriesName]) {
                result.Id = series.Id;
                result.SeriesName = series.SeriesName;

                try {
                    TvDbResponse<EpisodeRecord[]> episodeResponse = await tvdb.Series.GetEpisodesAsync(series.Id, 1,
                        new EpisodeQuery {
                            AiredSeason = episodeData.Season,
                            AiredEpisode = episodeData.Episode
                        }
                    );

                    result.Title = episodeResponse.Data[0].EpisodeName;
                    result.Overview = episodeResponse.Data[0].Overview;

                    break;
                } catch (TvDbServerException ex) {
                    if (series.Id == seriesResultCache[episodeData.SeriesName].Last().Id) {
                        if (config.verbose) {
                            setStatus($"Error: Cannot find {episodeData} ({ex.Message})", true);
                        } else {
                            setStatus($"Error: Cannot find {episodeData} on TheTVDB", true);
                        }
                        return false;
                    }
                }
            }

            setStatus($"Found {episodeData} ({result.Title}) on TheTVDB", false);

            TvDbResponse<TvDbSharper.Dto.Image[]> imagesResponse = null;

            if (config.addCoverArt) {
                try {
                    imagesResponse = await tvdb.Series.GetImagesAsync(result.Id, new ImagesQuery {
                            KeyType = KeyType.Season,
                            SubKey = episodeData.Season.ToString()
                        });
                } catch (TvDbServerException) {
                    try {
                        // use a series image if a season-specific image is not available
                        imagesResponse = await tvdb.Series.GetImagesAsync(result.Id, new ImagesQuery {
                            KeyType = KeyType.Series
                        });
                    } catch (TvDbServerException ex) {
                        if (config.verbose) {
                            setStatus($"Error: Failed to find episode cover - {ex.Message}", true);
                        } else {
                            setStatus("Error: Failed to find episode cover", true);
                        }
                        result.Complete = false;
                    }
                }
            }

            string imageFilename = "";
            if (imagesResponse != null) {
                imageFilename = imagesResponse.Data
                    .OrderByDescending(img => img.RatingsInfo.Average)
                    .First().FileName; // Find highest rated image
            }
            #endregion

            result.CoverURL = (String.IsNullOrEmpty(imageFilename)) ? null : $"https://artworks.thetvdb.com/banners/{imageFilename}";

            result.CoverFilename = imageFilename.Split('/').Last();

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
