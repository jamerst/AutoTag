using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System;

namespace autotag.Core {
    public class EpisodeParser {
        // Based on SubtitleFetcher's filename parsing
        // https://github.com/pheiberg/SubtitleFetcher

        static readonly string[] _patterns = {
            @"^((?<SeriesName>.+?)[\[. _-]+)?(?<Season>\d+)x(?<Episode>\d+)(([. _-]*x|-)(?<EndEpisode>(?!(1080|720)[pi])(?!(?<=x)264)\d+))*[\]. _-]*((?<ExtraInfo>.+?)((?<![. _-])-(?<ReleaseGroup>[^-]+))?)?$",
            @"^((?<SeriesName>.+?)[. _-]+)?s(?<Season>\d+)[. _-]*e(?<Episode>\d+)(([. _-]*e|-)(?<EndEpisode>(?!(1080|720)[pi])\d+))*[. _-]*((?<ExtraInfo>.+?)((?<![. _-])-(?<ReleaseGroup>[^-]+))?)?$"

        };
        public static FileMetadata ParseEpisodeInfo(string fileName) {
            foreach (var pattern in _patterns) {
                var match = Regex.Match(fileName, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (!match.Success)
                    continue;

                var seriesName = match.Groups["SeriesName"].Value.Replace('.', ' ').Replace('_', ' ').Trim();
                var season = match.Groups["Season"].Value;
                var episode = match.Groups["Episode"].Value;
                var endEpisode = ExtractEndEpisode(match.Groups["EndEpisode"], int.Parse(episode));
                var releaseGroup = match.Groups["ReleaseGroup"].Value;

                if (String.IsNullOrWhiteSpace(seriesName)) {
                    throw new FormatException("Unable to parse series name from filename");
                } else if (String.IsNullOrWhiteSpace(season)) {
                    throw new FormatException("Unable to parse season from filename");
                } else if (String.IsNullOrWhiteSpace(episode)) {
                    throw new FormatException("Unable to parse episode from filename");
                }

                var parsedDetails = new FileMetadata(FileMetadata.Types.TV) {
                    SeriesName = seriesName,
                    Season = int.Parse(season),
                    Episode = int.Parse(episode)
                };

                return parsedDetails;
            }
            throw new FormatException("Unable to parse required information from filename");
        }

        private static int ExtractEndEpisode(Capture endEpisodeGroup, int episode) {
            return !string.IsNullOrEmpty(endEpisodeGroup.Value) ? int.Parse(endEpisodeGroup.Value) : episode;
        }
    }
}