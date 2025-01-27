using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;

namespace AutoTag.Core.TV;

/// <summary>
/// Container class to hold episode as well as related episode group search results
/// </summary>
public partial class ShowResults
{
    /* settings */
    [GeneratedRegex(@"^\S+\s+(?<episode>\d+)")]
    private static partial Regex EpisodeRegex();

    /* vars */
    public SearchTv TvSearchResult { get; }
    
    public bool HasEpisodeGroupMapping => _episodeGroupMappingTable is not null;
    private Dictionary<(int season, int episode), (int season, int episode)>? _episodeGroupMappingTable;

    /// <summary>
    /// Create simple ShowResult container with <see cref="SearchTv"/> base
    /// </summary>
    /// <param name="tvSearchResult"></param>
    public ShowResults(SearchTv tvSearchResult)
    {
        TvSearchResult = tvSearchResult;
    }

    public static implicit operator ShowResults(SearchTv tv) => new(tv);

    /// <summary>
    /// Generates <see cref="ShowResults"/> list from any <see cref="SearchTv"/> enumerable
    /// </summary>
    /// <param name="results">Result from tmdb client</param>
    public static List<ShowResults> FromSearchResults(IEnumerable<SearchTv> results)
    {
        return results.Select(result => (ShowResults)result).ToList();
    }


    /// <summary>
    /// Add optional episode group to tv result
    /// </summary>
    /// <param name="episodeGroup">Episode group fetched from tmdb api</param>
    /// <param name="failureReason">Reason for failure (if returns <see langword="false" />)</param>
    public bool AddEpisodeGroup(TvGroupCollection episodeGroup, [NotNullWhen(false)] out string? failureReason)
    {
        if (!TryGenerateMappingTable(episodeGroup, out var mappingTable, out failureReason)) return false;

        _episodeGroupMappingTable = mappingTable;
        return true;
    }

    /// <summary>
    /// Try to retrieve episode mapping for episode group order
    /// </summary>
    /// <param name="seasonNumber">Season number as defined in episode group</param>
    /// <param name="episodeNumber">Episode number as defined in episode group</param>
    /// <param name="numbering">Matching episode number and season of "standard" order</param>
    /// <returns>True if mapping exists, false if not</returns>
    public bool TryGetMapping(int seasonNumber, int episodeNumber,
        [NotNullWhen(true)] out (int Season, int Episode)? numbering)
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
    /// <param name="failureReason">Reason for failure (if returns <see langword="false" />)</param>
    /// <returns>True if successful, false if not</returns>
    private static bool TryGenerateMappingTable(TvGroupCollection collection,
        [NotNullWhen(true)] out Dictionary<(int season, int episode), (int season, int episode)>? parsedTable,
        [NotNullWhen(false)] out string? failureReason)
    {
        parsedTable = [];

        foreach (var tvGroup in collection.Groups)
        {
            // determine season number
            var sanitizedGroupName = tvGroup.Name.ToLower().Trim();
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

            if (!seasonNumber.HasValue)
            {
                failureReason = $@"Unable to parse season number from group name ""{tvGroup.Name}""";
                return false;
            }

            // create mapping
            foreach (var episode in tvGroup.Episodes)
            {
                var mappingIsUnique = parsedTable.TryAdd(
                    (seasonNumber.Value, episode.Order + 1), // order starts at 0, episodes at 1
                    (episode.SeasonNumber, episode.EpisodeNumber)
                );

                if (!mappingIsUnique)
                {
                    parsedTable = null;
                    failureReason = $"Duplicate season-episode mapping for {tvGroup.Name} episode {episode.Order + 1}";
                    return false;
                }
            }
        }

        failureReason = null;
        return true;
    }
}