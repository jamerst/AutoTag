using System.Text.RegularExpressions;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;

namespace AutoTag.Core.TV;

public class ShowResult
{
    public SearchTv TvSearchResult { get; }
    private TvGroupCollection? GroupCollection { get; set; }

    public ShowResult(SearchTv tvSearchResult)
    {
        TvSearchResult = tvSearchResult;
    } 
    
    /// <summary>
    /// Add optional episode group to tv result
    /// </summary>
    /// <param name="episodeGroup">Episode group fetched from tmdb api</param>
    public void AddEpisodeGroup(TvGroupCollection episodeGroup)
    {
        GroupCollection = episodeGroup;
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
}