namespace AutoTag.Core;

public class FileNameReplace
{
    public required string Replace { get; set; }
    public required string Replacement { get; set; }

    public string Apply(string str) => str.Replace(Replace, Replacement);

    public static IEnumerable<FileNameReplace> FromStrings(IEnumerable<string> strings)
    {
        if (strings.Count() % 2 != 0)
        {
            throw new ArgumentException("Collection must have an even number of elements", nameof(strings));
        }

        string? previous = null;
        foreach (string str in strings)
        {
            if (previous == null)
            {
                previous = str;
            }
            else
            {
                yield return new FileNameReplace
                {
                    Replace = previous,
                    Replacement = str
                };

                previous = null;
            }
        }
    }

    public static IEnumerable<FileNameReplace> FromDictionary(IDictionary<string, string> dict)
        => dict.Select(x => new FileNameReplace { Replace = x.Key, Replacement = x.Value });
}