using System.Text.RegularExpressions;
using AutoTag.Core.Config;

namespace AutoTag.Core.Files;

public interface IFileNamer
{
    (string Result, bool ReplacedInvalid) GetNewFileName(FileMetadata metadata);
}

public partial class FileNamer(AutoTagConfig config) : IFileNamer
{
    private static readonly char[] InvalidNtfsChars = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];

    private readonly char[] _invalidFilenameChars = GetInvalidFileNameChars(config);

    [GeneratedRegex(
        @"{(?<specifier>[A-z]+)(?:\:(?<specifierFormat>[^}]+))?}|%(?<legacySpecifier>\d+)(?:\:(?<legacySpecifierFormat>[0#]+))?")]
    private static partial Regex RenameRegex { get; }

    public (string Result, bool ReplacedInvalid) GetNewFileName(FileMetadata metadata)
    {
        var pattern = metadata.GetRenamePattern(config);

        var fields = metadata.GetRenameFields().ToList();
        var fieldsBySpecifier = fields.ToDictionary(f => f.Specifier);
        var fieldsByLegacySpecifier = fields
            .Where(f => f.LegacySpecifier is not null)
            .ToDictionary(f => f.LegacySpecifier!);

        var removedInvalid = false;
        var path = RenameRegex.Replace(pattern, m =>
        {
            IFileNameField? field = null;
            var formatGroup = "";
            if (m.Groups.TryGetValue("specifier", out var specifier) &&
                fieldsBySpecifier.TryGetValue(specifier.Value, out var f1))
            {
                field = f1;
                formatGroup = "specifierFormat";
            }
            else if (m.Groups.TryGetValue("legacySpecifier", out var advancedSpecifier) &&
                fieldsByLegacySpecifier.TryGetValue(advancedSpecifier.Value, out var f2))
            {
                field = f2;
                formatGroup = "legacySpecifierFormat";
            }

            if (field == null) return "";

            var (result, removed) =
                ApplyReplaces(field.GetFormattedValue(m.Groups.GetValueOrDefault(formatGroup)?.Value));

            removedInvalid |= removed;

            return result;
        });

        return (path, removedInvalid);
    }

    private (string Result, bool RemovedInvalid) ApplyReplaces(string value)
    {
        var result = value;
        foreach (var replace in config.FileNameReplaces)
        {
            result = replace.Apply(result);
        }

        var sanitisedName = RemoveInvalidFileNameChars(result);

        return (sanitisedName, sanitisedName != result);
    }

    private string RemoveInvalidFileNameChars(string fileName)
        => string.Concat(fileName.Where(c => !_invalidFilenameChars.Contains(c)));

    private static char[] GetInvalidFileNameChars(AutoTagConfig config)
        => [..Path.GetInvalidFileNameChars(), ..config.WindowsSafe ? InvalidNtfsChars : []];
}