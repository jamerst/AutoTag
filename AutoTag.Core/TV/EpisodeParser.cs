using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace AutoTag.Core.TV;
public class EpisodeParser
{
    // Based on SubtitleFetcher's filename parsing
    // https://github.com/pheiberg/SubtitleFetcher

    static RegexOptions RegexOptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
    private static readonly Regex AbsoluteEpisodePattern = new(@"^(?<SeriesName>.+?)[. _-]*\((?<AbsoluteEpisode>[1-9]\d{0,3})\)[. _-]*(?<ExtraInfo>.*)?$", RegexOptions);
    private static readonly Regex SquareBracketGroupRegex = new(@"\[[^\]]+\]", RegexOptions);
    private static readonly Regex MultiSpaceRegex = new(@"\s+", RegexOptions);
    static readonly Regex[] Patterns =
    [
        new(@"^((?<SeriesName>.+?)[\[. _-]+)?(?<Season>\d+)x(?<Episode>\d+)(([. _-]*x|-)(?<EndEpisode>(?!(1080|720)[pi])(?!(?<=x)264)\d+))*[\]. _-]*((?<ExtraInfo>.+?)((?<![. _-])-(?<ReleaseGroup>[^-]+))?)?$", RegexOptions),
        new(@"^((?<SeriesName>.+?)[. _-]+)?s(?<Season>\d+)[. _-]*e(?<Episode>\d+)(([. _-]*e|-)(?<EndEpisode>(?!(1080|720)[pi])\d+))*[. _-]*((?<ExtraInfo>.+?)((?<![. _-])-(?<ReleaseGroup>[^-]+))?)?$", RegexOptions),
        new(@"^((?<SeriesName>.+?)[. _-]+)?e(?<Episode>\d+)(([. _-]*e|-)(?<EndEpisode>(?!(1080|720)[pi])\d+))*[. _-]*((?<ExtraInfo>.+?)((?<![. _-])-(?<ReleaseGroup>[^-]+))?)?$", RegexOptions)
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

            var seriesName = NormaliseSeriesName(match.Groups["SeriesName"].Value);
            var season = match.Groups["Season"].Value;
            var episode = match.Groups["Episode"].Value;

            if (string.IsNullOrWhiteSpace(seriesName))
            {
                failureReason = "Unable to parse series name from filename";
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
                Season = string.IsNullOrWhiteSpace(season) ? 1 : int.Parse(season),
                Episode = int.Parse(episode)
            };

            return true;
        }

        if (TryParseAbsoluteEpisodeInfo(fileName, out metadata, out failureReason))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(failureReason))
        {
            return false;
        }

        failureReason = "Unable to parse required information from filename";
        return false;
    }

    private static bool TryParseAbsoluteEpisodeInfo(string fileName,
        [NotNullWhen(true)] out TVFileMetadata? metadata,
        out string? failureReason)
    {
        metadata = null;
        failureReason = null;

        var match = AbsoluteEpisodePattern.Match(Path.GetFileNameWithoutExtension(fileName));
        if (!match.Success)
        {
            return false;
        }

        if (!int.TryParse(match.Groups["AbsoluteEpisode"].Value, out var absoluteEpisode)
            || IsLikelyYear(absoluteEpisode))
        {
            return false;
        }

        var seriesName = NormaliseSeriesName(match.Groups["SeriesName"].Value);
        if (string.IsNullOrWhiteSpace(seriesName))
        {
            failureReason = "Unable to parse series name from filename";
            return false;
        }

        metadata = new TVFileMetadata
        {
            SeriesName = seriesName,
            Season = 0,
            Episode = absoluteEpisode,
            AbsoluteEpisode = absoluteEpisode
        };

        return true;
    }

    private static string NormaliseSeriesName(string seriesName)
    {
        seriesName = SquareBracketGroupRegex.Replace(seriesName, " ");
        seriesName = seriesName.Replace('.', ' ').Replace('_', ' ');

        foreach (var pattern in LanguageTermPatterns)
        {
            seriesName = Regex.Replace(seriesName, pattern, " ", RegexOptions);
        }

        return MultiSpaceRegex.Replace(seriesName, " ").Trim(' ', '.', '-', '_');
    }

    private static bool IsLikelyYear(int value) => value is >= 1900 and <= 2099;

    private static readonly string[] LanguageTermPatterns =
    [
        @"\bDublado\b",
        @"\bLegendado\b",
        @"\bDubbed\b",
        @"\bSubbed\b",
        @"\bSubtitles?\b",
        @"\bSubtitled\b",
        @"\bPT-BR\b",
        @"\bENG\b"
    ];
}
