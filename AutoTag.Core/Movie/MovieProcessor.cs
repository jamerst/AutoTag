using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;

namespace AutoTag.Core.Movie;
public class MovieProcessor : IProcessor
{
    private readonly TMDbClient _tmdb;
    private readonly AutoTagConfig _config;
    
    private IEnumerable<Genre> _genres = Enumerable.Empty<Genre>();

    public MovieProcessor(string apiKey, AutoTagConfig config)
    {
        _tmdb = new(apiKey)
        {
            DefaultLanguage = config.Language,
            DefaultImageLanguage = config.Language
        };
        _config = config;
    }

    public async Task<bool> ProcessAsync(
        TaggingFile file,
        FileWriter writer,
        IUserInterface ui
    )
    {
        if (!TryParseFileName(Path.GetFileName(file.Path), out string? title, out int? year))
        {
            ui.SetStatus("Error: Failed to parse required information from filename", MessageType.Error);
            return false;
        }

        if (_config.Verbose)
        {
            ui.SetStatus($"Parsed file as {title}", MessageType.Information);
        }

        var (findMovieResult, selectedResult) = await FindMovieAsync(title, year, ui);
        switch (findMovieResult)
        {
            case FindResult.Fail:
                return false;
            case FindResult.Skip:
                return true;
        }

        ui.SetStatus($"Found {selectedResult!.Title} ({selectedResult.ReleaseDate?.Year.ToString() ?? "unknown year"}) on TheMovieDB", MessageType.Information);

        var result = await GetMovieMetadataAsync(selectedResult, file.Taggable, ui);
        
        bool taggingSuccess = await writer.WriteAsync(file, result, ui);

        return taggingSuccess && result.Success && result.Complete;
    }

    private static readonly Regex _fileNameRegex = new(
        @"^((?<Title>.+?)[\\. _-]?)" + // get title by reading from start to a field (whichever field comes first)
        "?(" +
        @"([\(]?(?<Year>(19|20)[0-9]{2})[\)]?)|" + // year - extract for use in searching
        "([0-9]{3,4}(p|i))|" + // resolution (e.g. 1080p, 720i)
        @"((?:PPV\.)?[HPS]DTV|[. ](?:HD)?CAM[| ]|B[DR]Rip|[.| ](?:HD-?)?TS[.| ]|(?:PPV )?WEB-?DL(?: DVDRip)?|HDRip|DVDRip|CamRip|W[EB]Rip|BluRay|DvDScr|hdtv|REMUX|3D|Half-(OU|SBS)+|4K|NF|AMZN)|" + // rip type
        @"(xvid|[hx]\.?26[45]|AVC)|" + // video codec
        @"(MP3|DD5\.?1|Dual[\- ]Audio|LiNE|DTS[-HD]+|AAC[.-]LC|AAC(?:\.?2\.0)?|AC3(?:\.5\.1)?|7\.1|DDP5.1)|" + // audio codec
        "(REPACK|INTERNAL|PROPER)|" + // scene tags
        @"\.(mp4|m4v|mkv)$" + // file extensions
        ")"
    );
    private bool TryParseFileName(string fileName, [NotNullWhen(true)] out string? title, out int? year)
    {
        Match match = _fileNameRegex.Match(fileName);
        if (match.Success)
        {
            title = match.Groups["Title"].Value.Replace('.', ' ');
            
            var yearStr = match.Groups["Year"].Value;
            year = string.IsNullOrEmpty(yearStr)
                ? null
                : int.Parse(yearStr);
        }
        else
        {
            title = null;
            year = null;
        }

        return !string.IsNullOrEmpty(title);
    }

    private async Task<(FindResult, SearchMovie?)> FindMovieAsync(string title, int? year, IUserInterface ui)
    {
        SearchContainer<SearchMovie> searchResults;
        if (year.HasValue)
        {
            searchResults = await _tmdb.SearchMovieAsync(query: title, year: year.Value); // if year was parsed, use it to narrow down search further
        }
        else
        {
            searchResults = await _tmdb.SearchMovieAsync(query: title);
        }

        int selected = 0;

        if (_config.ManualMode)
        {
            int? selection = ui.SelectOption(
                searchResults.Results
                    .Select(m => (
                        m.Title,
                        m.ReleaseDate?.Year.ToString() ?? "Unknown"
                    )).ToList()
            );

            if (selection.HasValue)
            {
                selected = selection.Value;
            }
            else
            {
                ui.SetStatus("File skipped", MessageType.Warning);
                return (FindResult.Skip, null);
            }
        }
        else if (!searchResults.Results.Any())
        {
            ui.SetStatus($"Error: failed to find title {title} on TheMovieDB", MessageType.Error);
            return (FindResult.Fail, null);
        }

        return (FindResult.Success, searchResults.Results[selected]);
    }

    private async Task<MovieFileMetadata> GetMovieMetadataAsync(SearchMovie selectedResult, bool fileIsTaggable, IUserInterface ui)
    {
        var result = new MovieFileMetadata
        {
            Id = selectedResult.Id,
            Title = selectedResult.Title,
            Overview = selectedResult.Overview,
            CoverURL = string.IsNullOrEmpty(selectedResult.PosterPath)
                ? null
                : $"https://image.tmdb.org/t/p/original{selectedResult.PosterPath}",
            Date = selectedResult.ReleaseDate
        };

        if (!_genres.Any())
        {
            _genres = await _tmdb.GetMovieGenresAsync();
        }
        result.Genres = selectedResult.GenreIds.Select(gId => _genres.First(g => g.Id == gId).Name).ToList();

        if (_config.ExtendedTagging && fileIsTaggable)
        {
            var credits = await _tmdb.GetMovieCreditsAsync(selectedResult.Id);

            result.Director = credits.Crew.FirstOrDefault(c => c.Job == "Director")?.Name;
            result.Actors = credits.Cast.Select(c => c.Name).ToList();
            result.Characters = credits.Cast.Select(c => c.Character).ToList();
        }

        if (string.IsNullOrEmpty(result.CoverURL))
        {
            ui.SetStatus("Error: failed to fetch movie cover", MessageType.Error);
            result.Complete = false;
        }

        return result;
    }
    
    void IDisposable.Dispose()
    {
        _tmdb.Dispose();
        GC.SuppressFinalize(this);
    }
}
