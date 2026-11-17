using AutoTag.Core.Config;
using AutoTag.Core.Files;
using AutoTag.Core.Tagging;

namespace AutoTag.Core.Movie;

public class MovieFileMetadata : FileMetadata
{
    private const byte StikMovie = 9;
    public DateTime? Date { get; init; }

    public override Tags GetTags(AutoTagConfig config)
    {
        var tags = base.GetTags(config);

        tags.Year = Date?.Year;
        tags.AppleMediaType = StikMovie;

        return tags;
    }

    public override string GetRenamePattern(AutoTagConfig config) => config.MovieRenamePattern;

    public override IEnumerable<IFileNameField> GetRenameFields()
    {
        yield return new StringFileNameField("Title", "1", Title);
        yield return new IntegerFileNameField("Year", "2", Date?.Year);
    }

    public override string ToString() => $"{Title}{(Date.HasValue ? $" ({Date.Value.Year})" : "")}";
}