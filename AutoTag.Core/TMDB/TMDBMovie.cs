using TMDbLib.Objects.Search;

namespace AutoTag.Core.TMDB;

public class TMDBMovie
{
    private TMDBMovie()
    {
    }

    public int Id { get; private init; }

    public string Title { get; private init; } = null!;

    public string Overview { get; private init; } = null!;

    public string? PosterPath { get; private init; }

    public DateTime? ReleaseDate { get; private init; }

    public List<string> Genres { get; private init; } = null!;

    public string Language { get; private init; } = null!;

    public static TMDBMovie FromSearchMovie(SearchMovie movie, string language, Dictionary<int, string> genreLookup) =>
        new()
        {
            Id = movie.Id,
            Title = movie.Title!,
            Overview = movie.Overview!,
            PosterPath = movie.PosterPath,
            ReleaseDate = movie.ReleaseDate,
            Genres = movie.GenreIds!.Select(g => genreLookup[g]).ToList(),
            Language = language
        };

    public static TMDBMovie FromMovie(TMDbLib.Objects.Movies.Movie movie, string language) => new()
    {
        Id = movie.Id,
        Title = movie.Title!,
        Overview = movie.Overview!,
        PosterPath = movie.PosterPath,
        ReleaseDate = movie.ReleaseDate,
        Genres = movie.Genres!.Select(g => g.Name!).ToList(),
        Language = language
    };
}