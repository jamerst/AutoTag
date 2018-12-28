using System;
using System.IO;
using System.Media;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;


namespace AutoTag {
	class MovieProcessor {
		private DataGridViewRow row;
		private TableUtils utils;

		private TMDbClient tmdb;

		public MovieProcessor(DataGridView table, DataGridViewRow row, TMDbClient tmdb) {
			this.row = row;
			utils = new TableUtils(table);
			this.tmdb = tmdb;
		}

		public async Task<FileMetadata> process(frmMain mainForm) {
			FileMetadata result = new FileMetadata(FileMetadata.Types.Movie);

			#region "Filename parsing"
			String pattern =
				"^((?<Title>.+?)[\\. _-]?)" + // get title by reading from start to a field (whichever field comes first)
				"?(" +
					"([\\(]?(?<Year>(19|20)[0-9]{2})[\\)]?)|" + // year - extract for use in searching
					"([0-9]{3,4}(p|i))|" + // resolution (e.g. 1080p, 720i)
					"((?:PPV\\.)?[HPS]DTV|[. ](?:HD)?CAM[| ]|B[DR]Rip|[.| ](?:HD-?)?TS[.| ]|(?:PPV )?WEB-?DL(?: DVDRip)?|HDRip|DVDRip|CamRip|W[EB]Rip|BluRay|DvDScr|hdtv|REMUX|3D|Half-(OU|SBS)+|4K|NF|AMZN)|" + // rip type
					"(xvid|[hx]\\.?26[45]|AVC)|" + // video codec
					"(MP3|DD5\\.?1|Dual[\\- ]Audio|LiNE|DTS[-HD]+|AAC[.-]LC|AAC(?:\\.?2\\.0)?|AC3(?:\\.5\\.1)?|7\\.1|DDP5.1)|" + // audio codec
					"(REPACK|INTERNAL|PROPER)|" + // scene tags
					"\\.(mp4|m4v|mkv)$" + // file extensions
				")";

			Match match = Regex.Match(Path.GetFileName(row.Cells[0].Value.ToString()), pattern);
			String title, year;
			if (match.Success) {
				title = match.Groups["Title"].ToString();
				year = match.Groups["Year"].ToString();
			} else {
				utils.SetRowError(row, "Error: Failed to parse required information from filename");
				result.Success = false;
				return result;
			}

			title = title.Replace('.', ' '); // change dots to spaces

			if (String.IsNullOrWhiteSpace(title)) {
				utils.SetRowError(row, "Error: Failed to parse required information from filename");
				result.Success = false;
				return result;
			}

			utils.SetRowStatus(row, "Parsed file as " + title);
			#endregion

			#region "TMDB API Searching"
			SearchContainer<SearchMovie> searchResults;
			if (!String.IsNullOrWhiteSpace(year)) {
				searchResults = await tmdb.SearchMovieAsync(query: title, year: int.Parse(year)); // if year was parsed, use it to narrow down search further
			}
			else {
				searchResults = await tmdb.SearchMovieAsync(query: title);
			}
			
			int selected = 0;

			// TMDb's search is not very sensitive, so it's very common for many results to be returned, so show a window to let the user select the correct movie if more than one is returned, but only if the first result doesn't match the parsed title
			if (searchResults.Results.Count > 1 && searchResults.Results[0].Title != title) {
				SystemSounds.Asterisk.Play();
				frmChoose chooseDialog = new frmChoose(title, searchResults);
				mainForm.Invoke(new MethodInvoker(() => chooseDialog.ShowDialog()));
				selected = chooseDialog.selectedIndex;
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
