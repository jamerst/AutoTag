using AutoTag.Core.Config;
using AutoTag.Core.Files;
using AutoTag.Core.TMDB;
using TMDbLib.Objects.Search;
using TMDbMovie = TMDbLib.Objects.Movies.Movie;

namespace AutoTag.Core.Movie;
public class MovieProcessor(ITMDBService tmdb, IFileWriter writer, IUserInterface ui, AutoTagConfig config) : IProcessor
{
    public async Task<bool> ProcessAsync(TaggingFile file)
    {
        if (MovieNameNormalizer.LooksLikeTvEpisode(Path.GetFileName(file.Path)))
        {
            ui.SetStatus("File skipped - filename looks like a TV episode", MessageType.Warning);
            return true;
        }

        if (!MovieNameNormalizer.TryParseFileName(Path.GetFileName(file.Path), out string? title, out int? year))
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

    private async Task<(FindResult, SearchMovie?)> FindMovieAsync(string title, int? year)
    {
        var manualResults = new List<SearchMovie>();
        var seenResultIds = new HashSet<int>();

        foreach (var attempt in GetSearchAttempts(title, year))
        {
            ui.DisplayMessage(
                $@"Searching TMDB for movie ""{attempt.Query}""{(attempt.Year.HasValue ? $" ({attempt.Year.Value})" : "")}{(string.Equals(attempt.Language, config.Language, StringComparison.OrdinalIgnoreCase) ? "" : $" [{attempt.Language}]")}",
                MessageType.Log
            );

            var searchResults = attempt.Year.HasValue
                ? await tmdb.SearchMovieAsync(attempt.Query, attempt.Year.Value, attempt.Language)
                : await tmdb.SearchMovieAsync(attempt.Query, language: attempt.Language);

            if (searchResults.Results.Count == 0)
            {
                continue;
            }

            if (!config.ManualMode)
            {
                return (FindResult.Success, searchResults.Results[0]);
            }

            foreach (var result in searchResults.Results.Where(result => seenResultIds.Add(result.Id)))
            {
                manualResults.Add(result);
            }
        }

        if (manualResults.Count == 0)
        {
            ui.SetStatus($"Error: failed to find title {title} on TheMovieDB", MessageType.Error);
            return (FindResult.Fail, null);
        }

        var selection = ui.SelectOption(
            "Please choose an option",
            manualResults
                .Select(m => $"{m.Title} ({m.ReleaseDate?.Year.ToString() ?? "Unknown"})")
                .ToList()
        );
        
        if (selection.HasValue)
        {
            var selected = manualResults[selection.Value];
            ui.SetStatus($"Selected {selected.Title} ({selected.ReleaseDate?.Year.ToString() ?? "Unknown"})", MessageType.Information);
            
            return (FindResult.Success, selected);
        }

        ui.SetStatus("File skipped", MessageType.Warning);
        return (FindResult.Skip, null);
    }

    private async Task<MovieFileMetadata> GetMovieMetadataAsync(SearchMovie selectedResult, bool fileIsTaggable)
    {
        TMDbMovie movie = await tmdb.GetMovieAsync(selectedResult.Id);
        var result = new MovieFileMetadata
        {
            Id = selectedResult.Id,
            Title = movie.Title,
            Overview = movie.Overview,
            CoverURL = string.IsNullOrEmpty(movie.PosterPath)
                ? null
                : $"https://image.tmdb.org/t/p/original{movie.PosterPath}",
            Date = movie.ReleaseDate
        };
        
        result.Genres = movie.Genres.Select(g => g.Name).ToList();

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

    private IEnumerable<MovieSearchAttempt> GetSearchAttempts(string title, int? year)
    {
        foreach (var candidate in MovieNameNormalizer.GetSearchCandidates(title))
        {
            foreach (var language in GetSearchLanguages())
            {
                if (year.HasValue)
                {
                    yield return new MovieSearchAttempt(candidate, year, language);
                }

                yield return new MovieSearchAttempt(candidate, null, language);
            }
        }
    }

    private IEnumerable<string> GetSearchLanguages()
    {
        var languages = new List<string>();

        if (!string.IsNullOrWhiteSpace(config.Language))
        {
            languages.Add(config.Language);
        }

        languages.AddRange(config.SearchLanguages.Where(language => !string.IsNullOrWhiteSpace(language)));

        return languages.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private readonly record struct MovieSearchAttempt(string Query, int? Year, string Language);
}
