using System.Text.RegularExpressions;

namespace AutoTag.Core.Files.Parsing;

public record ParsedMovieFileName(string Title, int? Year)
{
    public override string ToString() => $"{Title}{(Year.HasValue ? $" ({Year})" : "")}";
}

public class MovieFileNameParser
{
    private const RegexOptions SharedRegexOptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
    private const RegexOptions DomainTailRegexOptions = RegexOptions.CultureInvariant;

    private static readonly Regex SiteMarkerDomainTailRegex =
        new(
            @"[._\-\s]+(?:dir|www\d?|site|blog)(?:[._\-\s]+[a-z0-9]{2,}){0,8}\.(?:com|net|org|info|biz|co|io|tv|me|cc|ws|lt|mx|am)$",
            DomainTailRegexOptions);

    private static readonly Regex DomainTailRegex =
        new(@"[._\-\s]+[a-z0-9]{2,}(?:[._-][a-z0-9]{2,}){0,2}\.(?:com|net|org|info|biz|co|io|tv|me|cc|ws|lt|mx|am)$",
            DomainTailRegexOptions);

    private static readonly Regex TrailingReleaseTokenRegex = new(@"\s+[._-][a-z0-9]{2,20}$", SharedRegexOptions);
    private static readonly Regex SquareBracketGroupRegex = new(@"\[[^\]]+\]", SharedRegexOptions);

    private static readonly Regex ParenthesisedYearRegex =
        new(@"\((?:[^,)]*,\s*)?(?<Year>(19|20)\d{2})\)", SharedRegexOptions);

    private static readonly Regex BareYearRegex = new(@"\b(?<Year>(19|20)\d{2})\b", SharedRegexOptions);
    private static readonly Regex SeparatorRegex = new("[._-]+", SharedRegexOptions);
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

    public bool TryParse(string filePath, [NotNullWhen(true)] out ParsedMovieFileName? result)
    {
        var workingTitle = Path.GetFileNameWithoutExtension(filePath);
        if (string.IsNullOrWhiteSpace(workingTitle))
        {
            result = null;
            return false;
        }

        workingTitle = SiteMarkerDomainTailRegex.Replace(workingTitle, "");
        workingTitle = DomainTailRegex.Replace(workingTitle, "");
        workingTitle = SquareBracketGroupRegex.Replace(workingTitle, " ");

        int? year = null;
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

        var hadTechnicalNoise = false;
        foreach (var pattern in TechnicalTermPatterns)
        {
            workingTitle = Regex.Replace(workingTitle, pattern,
                _ =>
                {
                    hadTechnicalNoise = true;
                    return " ";
                },
                SharedRegexOptions
            );
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

        if (!string.IsNullOrWhiteSpace(workingTitle))
        {
            result = new ParsedMovieFileName(workingTitle, year);
            return true;
        }

        result = null;
        return false;
    }

    private static bool TryExtractParenthesisedYear(string title, [NotNullWhen(true)] out int? year,
        [NotNullWhen(true)]
        out string? updatedTitle)
    {
        year = null;
        updatedTitle = null;

        var match = ParenthesisedYearRegex.Match(title);
        if (!match.Success)
        {
            return false;
        }

        var candidateTitle = ParenthesisedYearRegex.Replace(title, " ", 1);
        if (!HasUsefulTitle(candidateTitle))
        {
            return false;
        }

        year = int.Parse(match.Groups["Year"].Value);
        updatedTitle = candidateTitle;
        return true;
    }

    private static bool TryExtractTrailingYear(string title, [NotNullWhen(true)] out int? year,
        [NotNullWhen(true)]
        out string? updatedTitle)
    {
        year = null;
        updatedTitle = null;

        var matches = BareYearRegex.Matches(title);
        if (matches.Count == 0)
        {
            return false;
        }

        var match = matches[^1];
        if (match.Index == 0)
        {
            return false;
        }

        var candidateTitle = title.Remove(match.Index, match.Length);
        if (!HasUsefulTitle(candidateTitle))
        {
            return false;
        }

        year = int.Parse(match.Groups["Year"].Value);
        updatedTitle = candidateTitle;
        return true;
    }

    private static bool HasUsefulTitle(string title) =>
        !string.IsNullOrWhiteSpace(MultiSpaceRegex.Replace(title, " ").Trim(TrimCharacters));
}