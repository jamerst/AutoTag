using AutoTag.Core.Config;
using AutoTag.Core.Files;
using TagLib;
using TagLib.Mpeg4;
using File = TagLib.File;

namespace AutoTag.Core.Movie;

public class MovieFileMetadata : FileMetadata
{
    private const byte StikMovie = 9;
    public DateTime? Date { get; init; }

    public override void WriteToFile(File file, AutoTagConfig config, IUserInterface ui)
    {
        base.WriteToFile(file, config, ui);

        if (Date.HasValue)
        {
            file.Tag.Year = (uint)Date.Value.Year;
        }

        if (config.AppleTagging && (file.TagTypes & TagTypes.Apple) == TagTypes.Apple)
        {
            var appleTags = (AppleTag)file.GetTag(TagTypes.Apple);

            // Media Type - allows Apple software to recognise as a movie
            appleTags.SetData("stik", new ByteVector(StikMovie), (uint)AppleDataBox.FlagType.ContainsData);
        }
    }

    public override string GetRenamePattern(AutoTagConfig config) => config.MovieRenamePattern;

    public override IEnumerable<IFileNameField> GetRenameFields()
    {
        yield return new StringFileNameField("Title", "1", Title);
        yield return new IntegerFileNameField("Year", "2", Date?.Year);
    }

    public override string ToString() => $"{Title}{(Date.HasValue ? $" ({Date.Value.Year})" : "")}";
}