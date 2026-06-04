using AutoTag.Core.Config;
using AutoTag.Core.Files;
using AutoTag.Core.TMDB;

namespace AutoTag.Core.Movie;

public class MovieProcessor(ITMDBService tmdb, IFileWriter writer, IUserInterface ui, AutoTagConfig config) : IProcessor
{
    public async Task<ProcessResult> ProcessAsync(TaggingFile file)
    {
        if (file.MovieDetails is null)
        {
            return ProcessResult.ParseFailure;
        }

        ui.SetStatus($"Parsed file as {file.MovieDetails}", MessageType.Log);

        var (findMovieResult, selectedResult) = await FindMovieAsync(file.MovieDetails.Title, file.MovieDetails.Year);
        switch (findMovieResult)
        {
            case FindResult.Fail:
                return ProcessResult.NotFound;
            case FindResult.Skip:
                return ProcessResult.Skipped;
        }

        ui.SetStatus(
            $"Found {selectedResult!.Title} ({selectedResult.ReleaseDate?.Year.ToString() ?? "unknown year"}) on TheMovieDB",
            MessageType.Information);

        var result = await GetMovieMetadataAsync(selectedResult, file.Taggable);

        var taggingSuccess = await writer.WriteAsync(file, result);

        return taggingSuccess && result.Complete
            ? ProcessResult.Success
            : ProcessResult.Fail;
    }

    private async Task<(FindResult, TMDBMovie?)> FindMovieAsync(string title, int? year)
    {
        var manualResults = new List<TMDBMovie>();
        var seenResultIds = new HashSet<int>();

        foreach (var attempt in GetSearchAttempts(title, year))
        {
            ui.DisplayMessage(
                $"Searching TheMovieDB for movie {attempt.ToString(config)}",
                MessageType.Log
            );

            var searchResults = await tmdb.SearchMovieAsync(attempt.Query, attempt.Language, attempt.Year);
            if (searchResults.Count == 0)
            {
                continue;
            }

            if (!config.ManualMode)
            {
                return (FindResult.Success, searchResults[0]);
            }

            foreach (var result in searchResults.Where(result => seenResultIds.Add(result.Id)))
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
            ui.SetStatus($"Selected {selected.Title} ({selected.ReleaseDate?.Year.ToString() ?? "Unknown"})",
                MessageType.Information);

            return (FindResult.Success, selected);
        }

        ui.SetStatus("File skipped", MessageType.Warning);
        return (FindResult.Skip, null);
    }

    private async Task<MovieFileMetadata> GetMovieMetadataAsync(TMDBMovie selectedResult, bool fileIsTaggable)
    {
        // refetch in metadata language if search language was different
        var movie = selectedResult.Language == config.Language
            ? selectedResult
            : await tmdb.GetMovieAsync(selectedResult.Id);

        var result = new MovieFileMetadata
        {
            Id = selectedResult.Id,
            Title = movie.Title,
            Overview = movie.Overview,
            CoverURL = string.IsNullOrEmpty(movie.PosterPath)
                ? null
                : $"https://image.tmdb.org/t/p/original{movie.PosterPath}",
            Date = movie.ReleaseDate,
            Genres = movie.Genres
        };

        if (config.ExtendedTagging && fileIsTaggable)
        {
            var credits = await tmdb.GetMovieCreditsAsync(selectedResult.Id);

            result.Director = credits.Crew!.FirstOrDefault(c => c.Job == "Director")?.Name;
            result.Actors = credits.Cast!.Select(c => c.Name!).ToList();
            result.Characters = credits.Cast!.Select(c => c.Character!).ToList();
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
        foreach (var candidate in GetSearchCandidates(title))
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

    private static HashSet<string> GetSearchCandidates(string title)
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            title
        };

        var words = title.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length > 4)
        {
            for (var removedWords = 1; removedWords <= Math.Min(3, words.Length - 3); removedWords++)
            {
                candidates.Add(string.Join(' ', words.Take(words.Length - removedWords)));
            }
        }

        return candidates;
    }

    private IEnumerable<string> GetSearchLanguages()
    {
        IEnumerable<string> languages = [config.Language, ..config.SearchLanguages];

        return languages.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private readonly record struct MovieSearchAttempt(string Query, int? Year, string Language)
    {
        public string ToString(AutoTagConfig config) =>
            $"""
             "{Query}"{(Year.HasValue ? $" ({Year.Value})" : "")}{(Language.ToLower() != config.Language ? $" [{Language}]" : "")}
             """;
    }
}