using System.Text.RegularExpressions;
using AutoTag.Core.Config;

namespace AutoTag.Core.Files.Parsing;

public record ParsedTVFileName(string SeriesName, int? Year, int? Season, int Episode, int? EndEpisode, int? Part)
{
    public override string ToString() =>
        $"{SeriesName} {(Season.HasValue ? $"S{Season:00}" : "")}E{Episode:00}{(EndEpisode.HasValue ? $"-{EndEpisode:00}" : "")}";
}

public partial class TVFileNameParser(AutoTagConfig config)
{
    [GeneratedRegex(
        @"^(?<SeriesName>(?!s\d).{2,}?)[._ -]*(?:\(?(?<Year>(?:19|20)\d{2})\)?)?[._ -]*(?:(?:s?(?<Season>\d+)[ex._ ](?<Episode>\d+\b))|(?:[e(]?(?<AbsoluteEpisode>\d+\b(?!.*s?\d+[ex]\d+))))(?:-e?(?<EndEpisode>\d+))?(?:.*p(?:ar)?t(?<Part>\d+))?.*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
    )]
    private static partial Regex TVRegex { get; }

    [GeneratedRegex(@"[.\-_ ]+")]
    private static partial Regex SeparatorRegex { get; }

    [GeneratedRegex(@"\b(?:dublado|legendado|[ds]ubbed|subtitle[sd]?|pt-br|eng)\b|\[[^\[]+\]",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TitleRemoveRegex { get; }

    public bool TryParse(string filePath, [NotNullWhen(true)] out ParsedTVFileName? result)
    {
        var match = Match(filePath);
        if (match.Success)
        {
            var year = match.Groups.GetNullableIntValue("Year");
            var season = match.Groups.GetNullableIntValue("Season");
            var episode = match.Groups.GetNullableIntValue("Episode");
            var absoluteEpisode = match.Groups.GetNullableIntValue("AbsoluteEpisode");

            // try to distinguish between a year and an absolute numbered episode
            if (!season.HasValue && !episode.HasValue &&
                (!absoluteEpisode.HasValue || (!year.HasValue && absoluteEpisode is > 1900 and < 2099)))
            {
                result = null;
                return false;
            }

            result = new ParsedTVFileName(
                CleanupTitle(match.Groups["SeriesName"].Value),
                year,
                season,
                episode ?? absoluteEpisode!.Value,
                match.Groups.GetNullableIntValue("EndEpisode"),
                match.Groups.GetNullableIntValue("Part")
            );

            return true;
        }

        result = null;
        return false;
    }

    private Match Match(string filePath)
    {
        if (string.IsNullOrEmpty(config.ParsePattern))
        {
            return TVRegex.Match(Path.GetFileNameWithoutExtension(filePath));
        }

        var regex = new Regex(config.ParsePattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var match = regex.Match(filePath);

        if (!match.Success)
        {
            if (config.ParsePattern.Contains('/') && Path.DirectorySeparatorChar != '/')
            {
                return regex.Match(filePath.Replace(Path.DirectorySeparatorChar, '/'));
            }

            if (config.ParsePattern.Contains(@"\\") && Path.DirectorySeparatorChar != '\\')
            {
                return regex.Match(filePath.Replace(Path.DirectorySeparatorChar, '\\'));
            }
        }

        return match;
    }

    private static string CleanupTitle(string title) =>
        SeparatorRegex.Replace(TitleRemoveRegex.Replace(title, ""), " ").Trim();
}