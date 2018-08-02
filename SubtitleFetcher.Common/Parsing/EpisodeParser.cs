using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System;

namespace SubtitleFetcher.Common.Parsing
{
    public class EpisodeParser : IEpisodeParser
    {
        private static readonly string[] RecognizedTags = 
            { "PROPER", "REPACK", "RERIP", "720p", "1080p", "WEB-DL",
              "H264", "x264", "H.264", "HDTV", "DD51", "DD5.1", "AAC2.0",
              "AAC20", "DL" };

        private static readonly string TagsPattern = CreateTagsPattern(RecognizedTags);

        private static string CreateTagsPattern(string[] recognizedTags)
        {
            string tags = string.Join("|", recognizedTags.Select(tag => $"({tag})"));
            return $"(?<Tags>({tags}))*";
        }

        readonly string[] _patterns = {
                                        @"^((?<SeriesName>.+?)[\[. _-]+)?(?<Season>\d+)x(?<Episode>\d+)(([. _-]*x|-)(?<EndEpisode>(?!(1080|720)[pi])(?!(?<=x)264)\d+))*[\]. _-]*((?<ExtraInfo>.+?)((?<![. _-])-(?<ReleaseGroup>[^-]+))?)?$",
                                        @"^((?<SeriesName>.+?)[. _-]+)?s(?<Season>\d+)[. _-]*e(?<Episode>\d+)(([. _-]*e|-)(?<EndEpisode>(?!(1080|720)[pi])\d+))*[. _-]*((?<ExtraInfo>.+?)((?<![. _-])-(?<ReleaseGroup>[^-]+))?)?$"
                                      };
        public TvReleaseIdentity ParseEpisodeInfo(string fileName)
        {
            foreach (var pattern in _patterns)
            {
                var match = Regex.Match(fileName, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (!match.Success) 
                    continue;

                var seriesName = match.Groups["SeriesName"].Value.Replace('.', ' ').Replace('_', ' ').Trim();
                var season = match.Groups["Season"].Value;
				var episode = match.Groups["Episode"].Value;
                var endEpisode = ExtractEndEpisode(match.Groups["EndEpisode"], int.Parse(episode));
                var releaseGroup = match.Groups["ReleaseGroup"].Value;
                var extraInfo = GetTags(match.Groups["ExtraInfo"]);

				if (String.IsNullOrWhiteSpace(seriesName)) {
					throw new FormatException("Unable to parse series name from filename");
				} else if (String.IsNullOrWhiteSpace(season)) {
					throw new FormatException("Unable to parse season from filename");
				} else if (String.IsNullOrWhiteSpace(episode)) {
					throw new FormatException("Unable to parse episode from filename");
				}

				var releaseIdentity = new TvReleaseIdentity
                {
                    SeriesName = seriesName,
                    Season = int.Parse(season),
                    Episode = int.Parse(episode),
                    EndEpisode = endEpisode,
                    ReleaseGroup = releaseGroup
                };
                foreach (var tag in extraInfo)
                {
                    releaseIdentity.Tags.Add(tag);
                }

                return releaseIdentity;
            }
			throw new FormatException("Unable to parse required information from filename");
        }

        private static int ExtractEndEpisode(Capture endEpisodeGroup, int episode)
        {
            return !string.IsNullOrEmpty(endEpisodeGroup.Value) ? int.Parse(endEpisodeGroup.Value) : episode;
        }

        private static IEnumerable<string> GetTags(Capture extraInfoGroup)
        {
            char[] separators = { '.', ' ', '-', '_' };
            var extraInfo = extraInfoGroup.Value;
            var tags = extraInfo.Split(separators);
            return tags.Select(e => e.Trim());
        }
    }
}