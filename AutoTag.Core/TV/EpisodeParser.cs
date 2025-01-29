using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace AutoTag.Core.TV;
public class EpisodeParser
{
    // Based on SubtitleFetcher's filename parsing
    // https://github.com/pheiberg/SubtitleFetcher

    static RegexOptions RegexOptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
    static readonly Regex[] Patterns =
    [
        new(@"^((?<SeriesName>.+?)[\[. _-]+)?(?<Season>\d+)x(?<Episode>\d+)(([. _-]*x|-)(?<EndEpisode>(?!(1080|720)[pi])(?!(?<=x)264)\d+))*[\]. _-]*((?<ExtraInfo>.+?)((?<![. _-])-(?<ReleaseGroup>[^-]+))?)?$", RegexOptions),
        new(@"^((?<SeriesName>.+?)[. _-]+)?s(?<Season>\d+)[. _-]*e(?<Episode>\d+)(([. _-]*e|-)(?<EndEpisode>(?!(1080|720)[pi])\d+))*[. _-]*((?<ExtraInfo>.+?)((?<![. _-])-(?<ReleaseGroup>[^-]+))?)?$", RegexOptions)
    ];
    
    public static bool TryParseEpisodeInfo(string fileName,
        [NotNullWhen(true)] out TVFileMetadata? metadata,
        [NotNullWhen(false)] out string? failureReason)
    {
        metadata = null;
        
        foreach (var pattern in Patterns)
        {
            var match = pattern.Match(fileName);
            if (!match.Success)
                continue;

            var seriesName = match.Groups["SeriesName"].Value.Replace('.', ' ').Replace('_', ' ').Trim();
            var season = match.Groups["Season"].Value;
            var episode = match.Groups["Episode"].Value;

            if (string.IsNullOrWhiteSpace(seriesName))
            {
                failureReason = "Unable to parse series name from filename";
                return false;
            }
            else if (string.IsNullOrWhiteSpace(season))
            {
                failureReason = "Unable to parse season from filename";
                return false;
            }
            else if (string.IsNullOrWhiteSpace(episode))
            {
                failureReason = "Unable to parse episode from filename";
                return false;
            }

            failureReason = null;
            metadata = new  TVFileMetadata
            {
                SeriesName = seriesName,
                Season = int.Parse(season),
                Episode = int.Parse(episode)
            };

            return true;
        }

        failureReason = "Unable to parse required information from filename";
        return false;
    }
}