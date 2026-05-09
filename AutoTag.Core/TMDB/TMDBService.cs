using AutoTag.Core.Config;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;
using Credits = TMDbLib.Objects.Movies.Credits;

namespace AutoTag.Core.TMDB;

public interface ITMDBService
{
    Task<SearchContainer<SearchTv>> SearchTvShowAsync(string query);

    Task<TvShow> GetTvShowWithEpisodeGroupsAsync(int id);

    Task<TvGroupCollection?> GetTvEpisodeGroupsAsync(string id);

    Task<TvSeason?> GetTvSeasonAsync(int tvShowId, int seasonNumber);

    Task<List<string>> GetTvGenreNamesAsync(IEnumerable<int> genreIds);
    
    Task<CreditsWithGuestStars> GetTvEpisodeCreditsAsync(int tvShowId, int seasonNumber, int episodeNumber);

    Task<ImagesWithId> GetTvShowImagesAsync(int id);

    Task<List<TMDBMovie>> SearchMovieAsync(string query, string language, int? year);

    Task<TMDBMovie> GetMovieAsync(int movieId);

    Task<Credits> GetMovieCreditsAsync(int movieId);
}

public class TMDBService(TMDbClient client, AutoTagConfig config) : ITMDBService
{
    private Dictionary<int, string> TVGenres = [];
    private Dictionary<int, string> MovieGenres = [];
    
    public Task<SearchContainer<SearchTv>> SearchTvShowAsync(string query)
        => client.SearchTvShowAsync(query, config.Language, includeAdult: config.IncludeAdult);

    public Task<TvShow> GetTvShowWithEpisodeGroupsAsync(int id)
        => client.GetTvShowAsync(id, TvShowMethods.EpisodeGroups, config.Language);

    public Task<TvGroupCollection?> GetTvEpisodeGroupsAsync(string id)
        => client.GetTvEpisodeGroupsAsync(id, config.Language);

    public Task<TvSeason?> GetTvSeasonAsync(int tvShowId, int seasonNumber)
        => client.GetTvSeasonAsync(tvShowId, seasonNumber, language: config.Language);

    public async Task<List<string>> GetTvGenreNamesAsync(IEnumerable<int> genreIds)
    {
        if (TVGenres.Count == 0)
        {
            TVGenres = (await client.GetTvGenresAsync(config.Language))
                .ToDictionary(g => g.Id, g => g.Name);
        }

        return genreIds.Select(g => TVGenres[g]).ToList();
    }

    public Task<CreditsWithGuestStars> GetTvEpisodeCreditsAsync(int tvShowId, int seasonNumber, int episodeNumber)
        => client.GetTvEpisodeCreditsAsync(tvShowId, seasonNumber, episodeNumber, config.Language);

    public Task<ImagesWithId> GetTvShowImagesAsync(int id)
        => client.GetTvShowImagesAsync(id, $"{config.Language},null");

    public async Task<List<TMDBMovie>> SearchMovieAsync(string query, string language, int? year)
    {
        var results = await client.SearchMovieAsync(query, language, includeAdult: config.IncludeAdult, year: year ?? 0);

        if (results.Results.Count == 0)
        {
            return [];
        }

        await GetMovieGenreNamesAsync();

        return results.Results.Select(r => TMDBMovie.FromSearchMovie(r, language, MovieGenres)).ToList();
    }

    public async Task<TMDBMovie> GetMovieAsync(int movieId)
        => TMDBMovie.FromMovie(await client.GetMovieAsync(movieId, language: config.Language), config.Language);

    
    private async Task GetMovieGenreNamesAsync()
    {
        if (MovieGenres.Count == 0)
        {
            MovieGenres = (await client.GetMovieGenresAsync(config.Language))
                .ToDictionary(g => g.Id, g => g.Name);
        }
    }

    public Task<Credits> GetMovieCreditsAsync(int movieId)
        => client.GetMovieCreditsAsync(movieId);
}
