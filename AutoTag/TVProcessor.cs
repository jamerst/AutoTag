using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using TvDbSharper;
using TvDbSharper.Dto;

using SubtitleFetcher.Common;
using SubtitleFetcher.Common.Parsing;

namespace AutoTag {
    public class TVProcessor : IProcessor {

        private ITvDbClient tvdb;
        private Dictionary<String, List<SeriesSearchResult>> seriesResultCache = new Dictionary<String, List<SeriesSearchResult>>(StringComparer.OrdinalIgnoreCase);

        public TVProcessor(ITvDbClient tvdb) {
            this.tvdb = tvdb;
        }

        public async Task<FileMetadata> process(TableUtils utils, DataGridViewRow row, frmMain mainForm) {
            FileMetadata result = new FileMetadata(FileMetadata.Types.TV);

            #region Filename parsing
            EpisodeParser parser = new EpisodeParser();
            TvReleaseIdentity episodeData;

            try {
                episodeData = parser.ParseEpisodeInfo(Path.GetFileName(row.Cells[0].Value.ToString())); // Parse info from filename
            } catch (FormatException ex) {
                utils.SetRowError(row, "Error: " + ex.Message);
                result.Success = false;
                return result;
            }

            result.Season = episodeData.Season;
            result.Episode = episodeData.Episode;

            utils.SetRowStatus(row, "Parsed file as " + episodeData);
            #endregion

            #region TVDB API searching
            if (!seriesResultCache.ContainsKey(episodeData.SeriesName)) { // if not already searched for series
                TvDbResponse<SeriesSearchResult[]> seriesIdResponse;
                try {
                    seriesIdResponse = await tvdb.Search.SearchSeriesByNameAsync(episodeData.SeriesName);
                } catch (TvDbServerException ex) {
                    utils.SetRowError(row, "Error: Cannot find series " + episodeData.SeriesName + Environment.NewLine + "(" + ex.Message + ")");
                    result.Success = false;
                    return result;
                }

                // sort results by similarity to parsed series name
                List<SeriesSearchResult> seriesResults = seriesIdResponse.Data
                    .OrderByDescending(
                        seriesResult => SeriesNameSimilarity(episodeData.SeriesName, seriesResult.SeriesName))
                    .ToList();

                seriesResultCache.Add(episodeData.SeriesName, seriesResults);
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
                        utils.SetRowError(row, "Error: Cannot find " + episodeData + Environment.NewLine + "(" + ex.Message + ")");
                        result.Success = false;
                        return result;
                    }
                }
            }

            utils.SetRowStatus(row, "Found " + episodeData + " (" + result.Title + ") on TheTVDB");

            ImagesQuery coverImageQuery = new ImagesQuery {
                KeyType = KeyType.Season,
                SubKey = episodeData.Season.ToString()
            };

            TvDbResponse<TvDbSharper.Dto.Image[]> imagesResponse = null;

            if (Properties.Settings.Default.addCoverArt == true) {
                try {
                    imagesResponse = await tvdb.Series.GetImagesAsync(result.Id, coverImageQuery);
                } catch (TvDbServerException ex) {
                    utils.SetRowError(row, "Error: Failed to find episode cover - " + ex.Message);
                    result.Complete = false;
                }
            }

            string imageFilename = "";
            if (imagesResponse != null) {
                imageFilename = imagesResponse.Data.OrderByDescending(img => img.RatingsInfo.Average).First().FileName; // Find highest rated image
            }
            #endregion

            result.CoverURL = (String.IsNullOrEmpty(imageFilename)) ? null : $"https://artworks.thetvdb.com/banners/{imageFilename}";

            result.CoverFilename = imageFilename.Split('/').Last();

            return result;
        }

        private double SeriesNameSimilarity(string parsedName, string seriesName) {
            if (seriesName.ToLower().Contains(parsedName.ToLower())) {
                return (double) parsedName.Length / (double) seriesName.Length;
            }

            return 0;
        }
    }
}
