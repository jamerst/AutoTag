namespace AutoTag.Core.Movie;

public class MovieFileMetadata : FileMetadata
{
    public DateTime? Date;

    public override void WriteToFile(TagLib.File file, AutoTagConfig config, IUserInterface ui)
    {
        base.WriteToFile(file, config, ui);

        if (Date.HasValue)
        {
            file.Tag.Year = (uint) Date.Value.Year;
        }

        if (config.AppleTagging && (file.TagTypes & TagLib.TagTypes.Apple) == TagLib.TagTypes.Apple)
        {
            var appleTags = (TagLib.Mpeg4.AppleTag) file.GetTag(TagLib.TagTypes.Apple);

            // Media Type - allows Apple software to recognise as a movie
            appleTags.SetData("stik", new TagLib.ByteVector(StikMovie), (uint) TagLib.Mpeg4.AppleDataBox.FlagType.ContainsData);
        }
    }

    private const byte StikMovie = 9;

    public override string GetFileName(AutoTagConfig config)
    {
        return RenameRegex.Replace(config.MovieRenamePattern, (m) =>
        {
            return m.Groups["num"].Value switch
            {
                "1" => Title!,
                "2" => Date.HasValue ? FormatRenameNumber(m, Date.Value.Year) : "",
                _ => m.Value
            };
        });
    }

    public override string ToString()
    {
        if (Date.HasValue)
        {
            return $"{Title} ({Date.Value.Year})";
        }
        else
        {
            return Title!;
        }
    }
}