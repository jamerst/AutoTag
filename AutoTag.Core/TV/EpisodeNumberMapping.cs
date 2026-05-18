using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;

namespace AutoTag.Core.TV;

public class EpisodeNumberMapping(List<TvSeason> seasons)
{
    public (TvSeason Season, TvSeasonEpisode Episode)? GetByEpisodeNumber(int episodeNumber)
    {
        var episodeCounter = 0;
        foreach (var season in seasons)
        {
            if (episodeNumber <= episodeCounter + season.Episodes.Count)
            {
                return (season, season.Episodes[episodeNumber - episodeCounter - 1]);
            }

            episodeCounter += season.Episodes.Count;
        }

        return null;
    }
}