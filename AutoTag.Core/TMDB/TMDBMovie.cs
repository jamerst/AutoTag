using TMDbLib.Objects.Search;

namespace AutoTag.Core.TMDB;

public class TMDBMovie
{
    public int Id { get; set; }
    
    public required string Title { get; set; }
    
    public required string Overview { get; set; }
    
    public string? PosterPath { get; set; }
    
    public DateTime? ReleaseDate { get; set; }
    
    public required List<string> Genres { get; set; }
    
    public required string Language { get; set; }
    
    private TMDBMovie() {}

    public static TMDBMovie FromSearchMovie(SearchMovie movie, string language, Dictionary<int, string> genreLookup) => new()
    {
        Id = movie.Id,
        Title = movie.Title,
        Overview = movie.Overview,
        PosterPath = movie.PosterPath,
        ReleaseDate = movie.ReleaseDate,
        Genres = movie.GenreIds.Select(g => genreLookup[g]).ToList(),
        Language = language
    };
    
    public static TMDBMovie FromMovie(TMDbLib.Objects.Movies.Movie movie, string language) => new()
    {
        Id = movie.Id,
        Title = movie.Title,
        Overview = movie.Overview,
        PosterPath = movie.PosterPath,
        ReleaseDate = movie.ReleaseDate,
        Genres = movie.Genres.Select(g => g.Name).ToList(),
        Language = language
    };
}