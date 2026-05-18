namespace AutoTag.Core.Files;

public class FileNameReplace(string replace, string replacement)
{
    private string Replace { get; } = replace;
    private string Replacement { get; } = replacement;

    public string Apply(string str) => str.Replace(Replace, Replacement);

    public static IEnumerable<FileNameReplace> FromDictionary(IDictionary<string, string> dict)
        => dict.Select(x => new FileNameReplace(x.Key, x.Value));
}