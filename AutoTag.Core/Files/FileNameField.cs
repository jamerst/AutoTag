using System.Text.RegularExpressions;

namespace AutoTag.Core.Files;

public interface IFileNameField
{
    string Specifier { get; }
    string? LegacySpecifier { get; }

    string GetFormattedValue(string? format);
}

public class StringFileNameField(string specifier, string? legacySpecifier, string? value) : IFileNameField
{
    private string? Value { get; } = value;
    public string Specifier { get; } = specifier;
    public string? LegacySpecifier { get; } = legacySpecifier;

    public string GetFormattedValue(string? _) => Value ?? "";
}

public class IntegerFileNameField(string specifier, string? legacySpecifier, int? value) : IFileNameField
{
    private static readonly Regex FormatSpecifierRegex = new("[0#]+");

    private int? Value { get; } = value;
    public string Specifier { get; } = specifier;
    public string? LegacySpecifier { get; } = legacySpecifier;

    public string GetFormattedValue(string? format)
    {
        if (string.IsNullOrEmpty(format))
        {
            return Value?.ToString() ?? "";
        }

        var split = format.Split('|', 2);

        if (split.Length == 2 && Value is null or 0)
        {
            return split[1];
        }

        return FormatSpecifierRegex.Replace(split[0], m => Value?.ToString(m.Value) ?? "");
    }
}