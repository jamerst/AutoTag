using System.Text.Json.Serialization;

namespace AutoTag.Core.Files;

public class FileNameReplace(string replace, string replacement)
{
    [JsonInclude]
    private string Replace { get; } = replace;

    [JsonInclude]
    private string Replacement { get; } = replacement;

    public string Apply(string str) => str.Replace(Replace, Replacement);

    public static IEnumerable<FileNameReplace> FromDictionary(IDictionary<string, string> dict)
        => dict.Select(x => new FileNameReplace(x.Key, x.Value));

    public override bool Equals(object? obj) =>
        obj is FileNameReplace r && r.Replace == Replace && r.Replacement == Replacement;

    public override int GetHashCode() => HashCode.Combine(Replace, Replacement);
}