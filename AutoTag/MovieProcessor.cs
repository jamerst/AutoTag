using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

using TMDbLib.Client;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.General;


namespace AutoTag {
	class MovieProcessor {
		private DataGridViewRow row;
		private TableUtils utils;

		private TMDbClient tmdb;

		public MovieProcessor(DataGridView table, DataGridViewRow row, TMDbClient tmdb) {
			this.row = row;
			this.utils = new TableUtils(table);
			this.tmdb = tmdb;
		}

		public async Task<FileMetadata> process() {
			FileMetadata result = new FileMetadata(FileMetadata.Types.Movie);

			#region "Filename parsing"
			String regStr = "([\\.| |_|-]?(" + // tag separators
				"([0-9]{3,4}p)|" + // resolution e.g. 720p or 1080p
				"((19|20)[0-9]{2})|" + // year
				"((?:PPV\\.)?[HPS]DTV|[.| ](?:HD)?CAM[.| ]|B[DR]Rip|[.| ](?:HD-?)?TS[.| ]|(?:PPV )?WEB-?DL(?: DVDRip)?|HDRip|DVDRip|CamRip|W[EB]BRip|BluRay|DvDScr|hdtv|REMUX|3D|Half-(OU|SBS)+|4K|NF|AMZN)|" + // rip type
				"((RUS|ITA|ENG)[\\.| |_|-|])|" + // common language abbreviations
				"([a-zA-z]{3}SUB)|" + // subtitles
				"(xvid|[hx]\\.?26[45]|AVC)|" + // video formats
				"(MP3|DD5\\.?1|Dual[\\- ]Audio|LiNE|DTS[-HD]+|AAC[.-]LC|AAC(?:\\.?2\\.0)?|AC3(?:\\.5\\.1)?|7\\.1|DDP5.1)|" + // audio formats
				"(REPACK|INTERNAL|PROPER)|" + // scene tags
				"(KILLERS|DIMENSION|SPARKS|MAJESTIC|YIFY|JYK|Hive|ROVERS|LOL|GHOULS|ETRG|FUM|BATV|W4F|RARBG|FGT|SPRiNTER|PSYCHD|EPSiLON|[\\[]ETTV[\\]])" + // release groups
				"))|(mp4|m4v|mkv)"; // file extensions

			String title = Regex.Replace(Path.GetFileName(row.Cells[0].Value.ToString()), regStr, "", RegexOptions.IgnoreCase); // (try to) remove anything from the file that isn't the title
			// the regex isn't perfect, but it should mostly parse common file name formats
			title = title.Replace('.', ' '); // change dots back to spaces

			if (String.IsNullOrWhiteSpace(title)) {
				utils.SetRowError(row, "Error: Failed to parse required information from filename");
				result.Success = false;
				return result;
			}

			utils.SetRowStatus(row, "Parsed file as " + title);
			#endregion

			#region "TMDB API Searching"
			SearchContainer<SearchMovie> searchResults = await tmdb.SearchMovieAsync(title);
			int selected = 0;

			if (searchResults.Results.Count > 1) { // TMDb's search is not very sensitive, so it's very common for many results to be returned, so show a window to let the user select the correct movie
				frmChoose chooseDialog = new frmChoose(searchResults);
				selected = chooseDialog.DisplayDialog();
			} else if (searchResults.Results.Count == 0) {
				utils.SetRowError(row, "Error: failed to find title " + title + "on TheMovieDB");
				result.Success = false;
				return result;
			}

			SearchMovie selectedResult = searchResults.Results[selected];

			utils.SetRowStatus(row, "Found " + selectedResult.Title + " (" + selectedResult.ReleaseDate.Value.Year + ")" + " on TheMovieDB");
			#endregion

			result.Title = selectedResult.Title;
			result.Overview = selectedResult.Overview;
			result.CoverURL = (String.IsNullOrEmpty(selectedResult.PosterPath)) ? null : "https://image.tmdb.org/t/p/original" + selectedResult.PosterPath;
			result.CoverFilename = selectedResult.PosterPath.Replace("/", "");
			result.Date = selectedResult.ReleaseDate.Value;

			if (String.IsNullOrEmpty(result.CoverURL)) {
				utils.SetRowError(row, "Error: failed to fetch movie cover");
				result.Complete = false;
			}

			return result;
		}
	}
}
