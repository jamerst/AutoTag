using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using TvDbSharper;
using TvDbSharper.Dto;

using SubtitleFetcher.Common;
using SubtitleFetcher.Common.Parsing;

namespace AutoTag {
	class TVProcessor {
		private DataGridViewRow row;
		private TableUtils utils;

		private ITvDbClient tvdb;

		public TVProcessor(DataGridView table, DataGridViewRow row, ITvDbClient tvdb) {
			this.row = row;
			this.utils = new TableUtils(table);
			this.tvdb = tvdb;
		}

		public async Task<FileMetadata> process() {
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

			utils.SetRowStatus(row, "Parsed file as " + episodeData);
			#endregion

			#region TVDB API searching
			TvDbResponse<SeriesSearchResult[]> seriesIdResponse;
			try {
				seriesIdResponse = await tvdb.Search.SearchSeriesByNameAsync(episodeData.SeriesName);
			} catch (TvDbServerException ex) {
				utils.SetRowError(row, "Error: Cannot find series " + episodeData.SeriesName + Environment.NewLine + "(" + ex.Message + ")");
				result.Success = false;
				return result;
			}

			var series = seriesIdResponse.Data[0];

			EpisodeQuery episodeQuery = new EpisodeQuery {
				AiredSeason = episodeData.Season,
				AiredEpisode = episodeData.Episode // Define query parameters
			};

			TvDbResponse<EpisodeRecord[]> episodeResponse;
			try {
				episodeResponse = await tvdb.Series.GetEpisodesAsync(series.Id, 1, episodeQuery);
			} catch (TvDbServerException ex) {
				utils.SetRowError(row, "Error: Cannot find " + episodeData + Environment.NewLine + "(" + ex.Message + ")");
				result.Success = false;
				return result;
			}

			EpisodeRecord foundEpisode = episodeResponse.Data.First();

			utils.SetRowStatus(row, "Found " + episodeData + " (" + foundEpisode.EpisodeName + ") on TheTVDB");

			ImagesQuery coverImageQuery = new ImagesQuery {
				KeyType = KeyType.Season,
				SubKey = episodeData.Season.ToString()
			};

			TvDbResponse<TvDbSharper.Dto.Image[]> imagesResponse = null;

			if (Properties.Settings.Default.addCoverArt == true) {
				try {
					imagesResponse = await tvdb.Series.GetImagesAsync(series.Id, coverImageQuery);
				} catch (TvDbServerException ex) {
					utils.SetRowError(row, "Error: Failed to find episode cover - " + ex.Message);
					result.Complete = false;
				}
			}

			string imageFilename = "";
			if (imagesResponse != null) {
				imageFilename = imagesResponse.Data.OrderByDescending(obj => obj.RatingsInfo.Average).First().FileName.Split('/').Last(); // Find highest rated image
			}
			#endregion

			result.Title = foundEpisode.EpisodeName;
			result.Overview = foundEpisode.Overview;
			result.CoverURL = (String.IsNullOrEmpty(imageFilename)) ? null : "https://www.thetvdb.com/banners/seasons/" + imageFilename;
			result.CoverFilename = imageFilename;
			result.SeriesName = series.SeriesName;
			result.Season = episodeData.Season;
			result.Episode = episodeData.Episode;

			return result;
		}

	}
}
