using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using AutoTag.Core.Files;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;

namespace AutoTag.Core.Movie;
public class MovieProcessor(TMDbClient tmdb, IFileWriter writer, IUserInterface ui, AutoTagConfig config) : IProcessor
{
    private IEnumerable<Genre> Genres = [];

    public async Task<bool> ProcessAsync(TaggingFile file)
    {
        if (!TryParseFileName(Path.GetFileName(file.Path), out string? title, out int? year))
        {
            ui.SetStatus("Error: Failed to parse required information from filename", MessageType.Error);
            return false;
        }
        
        ui.SetStatus($"Parsed file as {title}", MessageType.Log);

        var (findMovieResult, selectedResult) = await FindMovieAsync(title, year);
        switch (findMovieResult)
        {
            case FindResult.Fail:
                return false;
            case FindResult.Skip:
                return true;
        }

        ui.SetStatus($"Found {selectedResult!.Title} ({selectedResult.ReleaseDate?.Year.ToString() ?? "unknown year"}) on TheMovieDB", MessageType.Information);

        var result = await GetMovieMetadataAsync(selectedResult, file.Taggable);
        
        bool taggingSuccess = await writer.WriteAsync(file, result);

        return taggingSuccess && result.Success && result.Complete;
    }

    private static readonly Regex FileNameRegex = new(
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
        Match match = FileNameRegex.Match(fileName);
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

    private async Task<(FindResult, SearchMovie?)> FindMovieAsync(string title, int? year)
    {
        SearchContainer<SearchMovie> searchResults;
        if (year.HasValue)
        {
            searchResults = await tmdb.SearchMovieAsync(query: title, year: year.Value); // if year was parsed, use it to narrow down search further
        }
        else
        {
            searchResults = await tmdb.SearchMovieAsync(query: title);
        }

        SearchMovie selected = searchResults.Results[0];
        if (config.ManualMode)
        {
            int? selection = ui.SelectOption(
                "Please choose an option",
                searchResults.Results
                    .Select(m => $"{m.Title} ({m.ReleaseDate?.Year.ToString() ?? "Unknown"})")
                    .ToList()
            );

            if (selection.HasValue)
            {
                selected = searchResults.Results[selection.Value];
                ui.SetStatus($"Selected {selected.Title} ({selected.ReleaseDate?.Year.ToString() ?? "Unknown"})", MessageType.Information);
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

        return (FindResult.Success, selected);
    }

    private async Task<MovieFileMetadata> GetMovieMetadataAsync(SearchMovie selectedResult, bool fileIsTaggable)
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

        if (!Genres.Any())
        {
            Genres = await tmdb.GetMovieGenresAsync();
        }
        result.Genres = selectedResult.GenreIds.Select(gId => Genres.First(g => g.Id == gId).Name).ToList();

        if (config.ExtendedTagging && fileIsTaggable)
        {
            var credits = await tmdb.GetMovieCreditsAsync(selectedResult.Id);

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
}
