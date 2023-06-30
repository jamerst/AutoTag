using System.Text.RegularExpressions;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;

namespace AutoTag.Core.TV;

public partial class ShowResult
{
    /* settings */
    [GeneratedRegex("(volume|season)\\s(?<episode>\\d+)")]
    private static partial Regex EpisodeRegex();

    /* vars */
    public SearchTv TvSearchResult { get; }
    private TvGroupCollection? GroupCollection { get; set; }
    public IReadOnlyDictionary<(int season, int episode), (int season, int episode)>? EpisodeGroupMappingTable => _episodeGroupMappingTable;
    private Dictionary<(int season, int episode), (int season, int episode)>? _episodeGroupMappingTable;

    /// <summary>
    /// Create simple ShowResult container with <see cref="SearchTv"/> base
    /// </summary>
    /// <param name="tvSearchResult"></param>
    public ShowResult(SearchTv tvSearchResult)
    {
        TvSearchResult = tvSearchResult;
    }

    public static implicit operator ShowResult(SearchTv tv) => new(tv);

    /// <summary>
    /// Generates <see cref="ShowResult"/> list from any <see cref="SearchTv"/> enumerable 
    /// </summary>
    /// <param name="results">Result from tmdb client</param>
    public static List<ShowResult> FromSearchResults(IEnumerable<SearchTv> results)
    {
        return results.Select(result => (ShowResult)result).ToList();
    }

    
    /// <summary>
    /// Add optional episode group to tv result
    /// </summary>
    /// <param name="episodeGroup">Episode group fetched from tmdb api</param>
    public bool AddEpisodeGroup(TvGroupCollection episodeGroup)
    {
        if (!TryGenerateMappingTable(episodeGroup, out var mappingTable)) return false;

        _episodeGroupMappingTable = mappingTable;
        GroupCollection = episodeGroup;
        return true;
    }

    public bool TryGetMapping(int seasonNumber, int episodeNumber, out (int season, int episode)? numbering)
    {
        numbering = null;
        if (_episodeGroupMappingTable?.TryGetValue((seasonNumber, episodeNumber), out var result) ?? false)
        {
            numbering = result;
            return true;
        }

        return false;
    }
    
    /// <summary>
    /// Tries to generate mapping table between TMDB standard sorting of show
    /// and the given Episode Group
    /// </summary>
    /// <param name="collection">Episode Group from TMDB</param>
    /// <param name="parsedTable">Filled parsing table. Only filled when method returns true</param>
    /// <returns>True if successful, false if not</returns>
    private static bool TryGenerateMappingTable(TvGroupCollection collection,
        out Dictionary<(int season, int episode), (int season, int episode)>? parsedTable)
    {
        parsedTable = new Dictionary<(int season, int episode), (int season, int episode)>();

        foreach (var tvGroup in collection.Groups)
        {
            // determine season number
            var sanitizedGroupName = tvGroup.Name.Trim().ToLower();
            int? seasonNumber = null;

            var seasonMatch = EpisodeRegex().Match(sanitizedGroupName);
            if (seasonMatch.Success)
            {
                seasonNumber = int.Parse(seasonMatch.Groups["episode"].Value);
            }
            else if (sanitizedGroupName.StartsWith("special"))
            {
                seasonNumber = 0;
            }

            if (!seasonNumber.HasValue) return false;

            // create mapping
            foreach (var episode in tvGroup.Episodes)
            {
                parsedTable.Add(
                    (seasonNumber.Value, episode.Order + 1), // order starts at 0, episodes at 1
                    (episode.SeasonNumber, episode.EpisodeNumber));
            }
        }

        return true;
    }
}