using System.Diagnostics.CodeAnalysis;
using TMDbLib.Objects.TvShows;

namespace AutoTag.Core.TV;

public interface ITVCache
{
    bool ShowIsCached(string seriesName);
    
    void AddShow(string seriesName, List<ShowResults> show);
    
    List<ShowResults> GetShow(string seriesName);

    bool TryGetSeason(int showId, int seasonNumber, [NotNullWhen(true)] out TvSeason? season);
    
    void AddSeason(int showId, int seasonNumber, TvSeason season);

    bool TryGetSeasonPoster(int showId, int seasonNumber, [NotNullWhen(true)] out string? url);

    void AddSeasonPoster(int showId, int seasonNumber, string url);
}

public class TVCache : ITVCache
{
    private readonly Dictionary<string, List<ShowResults>> CachedShows = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(int, int), TvSeason> CachedSeasons = new();
    private readonly Dictionary<(int, int), string> CachedSeasonPosters = new();

    public bool ShowIsCached(string seriesName)
        => CachedShows.ContainsKey(seriesName);

    public void AddShow(string seriesName, List<ShowResults> show)
        => CachedShows.Add(seriesName, show);

    public List<ShowResults> GetShow(string seriesName)
        => CachedShows[seriesName];

    public bool TryGetSeason(int showId, int seasonNumber, [NotNullWhen(true)] out TvSeason? season)
        => CachedSeasons.TryGetValue((showId, seasonNumber), out season);

    public void AddSeason(int showId, int seasonNumber, TvSeason season)
        => CachedSeasons.Add((showId, seasonNumber), season);

    public bool TryGetSeasonPoster(int showId, int seasonNumber, [NotNullWhen(true)] out string? url)
        => CachedSeasonPosters.TryGetValue((showId, seasonNumber), out url);

    public void AddSeasonPoster(int showId, int seasonNumber, string url)
        => CachedSeasonPosters.Add((showId, seasonNumber), url);
}