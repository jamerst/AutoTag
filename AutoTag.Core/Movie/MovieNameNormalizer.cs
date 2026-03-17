using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace AutoTag.Core.Movie;

public static class MovieNameNormalizer
{
    public static bool LooksLikeTvEpisode(string fileName)
        => TVEpisodePatternRegex.IsMatch(Path.GetFileNameWithoutExtension(fileName));

    public static bool LooksLikeMovieCandidate(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(name) || LooksLikeTvEpisode(fileName))
        {
            return false;
        }

        return ParenthesisedYearRegex.IsMatch(name)
               || BareYearRegex.IsMatch(name)
               || SiteMarkerDomainTailRegex.IsMatch(name)
               || DomainTailRegex.IsMatch(name)
               || TechnicalTermPatterns.Any(pattern => Regex.IsMatch(name, pattern, SharedRegexOptions));
    }

    public static bool TryParseFileName(string fileName, [NotNullWhen(true)] out string? title, out int? year)
    {
        year = null;

        var workingTitle = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(workingTitle))
        {
            title = null;
            return false;
        }

        var hadTechnicalNoise = TechnicalTermPatterns.Any(pattern => Regex.IsMatch(workingTitle, pattern, SharedRegexOptions));

        workingTitle = SiteMarkerDomainTailRegex.Replace(workingTitle, "");
        workingTitle = DomainTailRegex.Replace(workingTitle, "");
        workingTitle = SquareBracketGroupRegex.Replace(workingTitle, " ");

        if (TryExtractParenthesisedYear(workingTitle, out var parenthesisedYear, out var withoutParenthesisedYear))
        {
            year = parenthesisedYear;
            workingTitle = withoutParenthesisedYear;
        }
        else if (TryExtractTrailingYear(workingTitle, out var trailingYear, out var withoutTrailingYear))
        {
            year = trailingYear;
            workingTitle = withoutTrailingYear;
        }

        foreach (var pattern in TechnicalTermPatterns)
        {
            workingTitle = Regex.Replace(workingTitle, pattern, " ", SharedRegexOptions);
        }

        foreach (var pattern in LanguageTermPatterns)
        {
            workingTitle = Regex.Replace(workingTitle, pattern, " ", SharedRegexOptions);
        }

        if (hadTechnicalNoise)
        {
            workingTitle = TrailingReleaseTokenRegex.Replace(workingTitle, "");
        }

        workingTitle = SeparatorRegex.Replace(workingTitle, " ");
        workingTitle = BracketCharacterRegex.Replace(workingTitle, " ");
        workingTitle = MultiSpaceRegex.Replace(workingTitle, " ").Trim();
        workingTitle = TrimmedPunctuationRegex.Replace(workingTitle, "");

        title = string.IsNullOrWhiteSpace(workingTitle)
            ? null
            : workingTitle;

        return title != null;
    }

    public static IReadOnlyList<string> GetSearchCandidates(string title)
    {
        var candidates = new List<string>();
        AddCandidate(candidates, title);

        var words = title.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length > 4)
        {
            for (int removedWords = 1; removedWords <= Math.Min(3, words.Length - 3); removedWords++)
            {
                AddCandidate(candidates, string.Join(' ', words.Take(words.Length - removedWords)));
            }
        }

        return candidates;
    }

    private static void AddCandidate(List<string> candidates, string candidate)
    {
        if (!string.IsNullOrWhiteSpace(candidate) &&
            !candidates.Contains(candidate, StringComparer.OrdinalIgnoreCase))
        {
            candidates.Add(candidate);
        }
    }

    private static bool TryExtractParenthesisedYear(string title, out int year, [NotNullWhen(true)] out string? updatedTitle)
    {
        var match = ParenthesisedYearRegex.Match(title);
        if (!match.Success)
        {
            year = default;
            updatedTitle = null;
            return false;
        }

        var candidateTitle = ParenthesisedYearRegex.Replace(title, " ", 1);
        if (!HasUsefulTitle(candidateTitle))
        {
            year = default;
            updatedTitle = null;
            return false;
        }

        year = int.Parse(match.Groups["Year"].Value);
        updatedTitle = candidateTitle;
        return true;
    }

    private static bool TryExtractTrailingYear(string title, out int year, [NotNullWhen(true)] out string? updatedTitle)
    {
        var matches = BareYearRegex.Matches(title);
        if (matches.Count == 0)
        {
            year = default;
            updatedTitle = null;
            return false;
        }

        var match = matches[matches.Count - 1];
        if (match.Index == 0)
        {
            year = default;
            updatedTitle = null;
            return false;
        }

        var candidateTitle = title.Remove(match.Index, match.Length);
        if (!HasUsefulTitle(candidateTitle))
        {
            year = default;
            updatedTitle = null;
            return false;
        }

        year = int.Parse(match.Groups["Year"].Value);
        updatedTitle = candidateTitle;
        return true;
    }

    private static bool HasUsefulTitle(string title)
        => !string.IsNullOrWhiteSpace(MultiSpaceRegex.Replace(title, " ").Trim(TrimCharacters));

    private const RegexOptions SharedRegexOptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
    private const RegexOptions DomainTailRegexOptions = RegexOptions.CultureInvariant;
    private static readonly Regex SiteMarkerDomainTailRegex = new(@"[._\-\s]+(?:dir|www\d?|site|blog)(?:[._\-\s]+[a-z0-9]{2,}){0,8}\.(?:com|net|org|info|biz|co|io|tv|me|cc|ws|lt|mx|am)$", DomainTailRegexOptions);
    private static readonly Regex DomainTailRegex = new(@"[._\-\s]+[a-z0-9]{2,}(?:[._-][a-z0-9]{2,}){0,2}\.(?:com|net|org|info|biz|co|io|tv|me|cc|ws|lt|mx|am)$", DomainTailRegexOptions);
    private static readonly Regex TrailingReleaseTokenRegex = new(@"\s+[._-][a-z0-9]{2,20}$", SharedRegexOptions);
    private static readonly Regex SquareBracketGroupRegex = new(@"\[[^\]]+\]", SharedRegexOptions);
    private static readonly Regex ParenthesisedYearRegex = new(@"\((?:[^,)]*,\s*)?(?<Year>(19|20)\d{2})\)", SharedRegexOptions);
    private static readonly Regex BareYearRegex = new(@"\b(?<Year>(19|20)\d{2})\b", SharedRegexOptions);
    private static readonly Regex TVEpisodePatternRegex = new(@"\b(?:s\d{1,2}\s*e\d{1,3}|\d{1,2}x\d{1,3}|episode\s*\d{1,3}|e\d{1,3})\b", SharedRegexOptions);
    private static readonly Regex SeparatorRegex = new(@"[._-]+", SharedRegexOptions);
    private static readonly Regex BracketCharacterRegex = new(@"[(){}\[\]]", SharedRegexOptions);
    private static readonly Regex MultiSpaceRegex = new(@"\s+", SharedRegexOptions);
    private static readonly Regex TrimmedPunctuationRegex = new(@"^[\s\p{P}]+|[\s\p{P}]+$", SharedRegexOptions);
    private static readonly char[] TrimCharacters = [' ', '.', '-', '_'];

    private static readonly string[] TechnicalTermPatterns =
    [
        @"\b\d{3,4}[pi]\b",
        @"\b4K\b",
        @"\bUHD\b",
        @"\bBluRay\b",
        @"\bBRRip\b",
        @"\bWEB-?DL\b",
        @"\bWEBRip\b",
        @"\bDVDRip\b",
        @"\bHDRip\b",
        @"\bBDRip\b",
        @"\bHDTV\b",
        @"\bAMZN\b",
        @"\bHMAX\b",
        @"\bNF\b",
        @"\bREMUX\b",
        @"\bx\.?264\b",
        @"\bx\.?265\b",
        @"\bHEVC\b",
        @"\bAVC\b",
        @"\bH\.?264\b",
        @"\bH\.?265\b",
        @"\bAAC(?:\d(?:\.\d)?)?\b",
        @"\bAC3(?:\.\d)?\b",
        @"\bDTS(?:-HD)?\b",
        @"\bFLAC\b",
        @"\bDDP\d(?:\.\d)?\b",
        @"\bTrueHD\b",
        @"\bAtmos\b",
        @"\bDual\s*Audio\b",
        @"\bMulti\s*Audio\b",
        @"\bRepack\b",
        @"\bProper\b",
        @"\bExtended\b",
        @"\bUnrated\b",
        @"\bInternal\b"
    ];

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
