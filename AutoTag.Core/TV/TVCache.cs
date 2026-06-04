using TMDbLib.Objects.TvShows;

namespace AutoTag.Core.TV;

public interface ITVCache
{
    void AddShow(string seriesName, int? year, List<ShowResults> show);

    bool TryGetShow(string seriesName, int? year, [NotNullWhen(true)] out List<ShowResults>? results);

    bool TryGetSeason(int showId, int seasonNumber, [NotNullWhen(true)] out TvSeason? season);

    void AddSeason(int showId, int seasonNumber, TvSeason season);

    bool TryGetSeasonPoster(int showId, int seasonNumber, [NotNullWhen(true)] out string? url);

    void AddSeasonPoster(int showId, int seasonNumber, string url);
}

public class TVCache : ITVCache
{
    private readonly Dictionary<(int, int), string> CachedSeasonPosters = new();
    private readonly Dictionary<(int, int), TvSeason> CachedSeasons = new();

    private readonly Dictionary<(string ShowName, int? Year), List<ShowResults>> CachedShows =
        new(new ShowYearComparer());

    public void AddShow(string seriesName, int? year, List<ShowResults> show)
        => CachedShows.Add((seriesName, year), show);

    public bool TryGetShow(string seriesName, int? year, [NotNullWhen(true)] out List<ShowResults>? results)
        => CachedShows.TryGetValue((seriesName, year), out results);

    public bool TryGetSeason(int showId, int seasonNumber, [NotNullWhen(true)] out TvSeason? season)
        => CachedSeasons.TryGetValue((showId, seasonNumber), out season);

    public void AddSeason(int showId, int seasonNumber, TvSeason season)
        => CachedSeasons.Add((showId, seasonNumber), season);

    public bool TryGetSeasonPoster(int showId, int seasonNumber, [NotNullWhen(true)] out string? url)
        => CachedSeasonPosters.TryGetValue((showId, seasonNumber), out url);

    public void AddSeasonPoster(int showId, int seasonNumber, string url)
        => CachedSeasonPosters.Add((showId, seasonNumber), url);
}

internal class ShowYearComparer : IEqualityComparer<(string ShowName, int? Year)>
{
    public bool Equals((string ShowName, int? Year) x, (string ShowName, int? Year) y)
        => StringComparer.OrdinalIgnoreCase.Equals(x.ShowName, y.ShowName) && x.Year == y.Year;

    public int GetHashCode((string ShowName, int? Year) obj) => HashCode.Combine(obj.ShowName, obj.Year);
}