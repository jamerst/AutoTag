namespace AutoTag.Core.Movie;

public class MovieFileMetadata : FileMetadata
{
    public DateTime? Date;

    public override void WriteToFile(TagLib.File file, AutoTagConfig config, Action<string, MessageType> setStatus)
    {
        base.WriteToFile(file, config, setStatus);

        if (Date.HasValue)
        {
            file.Tag.Year = (uint) Date.Value.Year;
        }

        if (config.AppleTagging && (file.TagTypes & TagLib.TagTypes.Apple) == TagLib.TagTypes.Apple)
        {
            var appleTags = (TagLib.Mpeg4.AppleTag) file.GetTag(TagLib.TagTypes.Apple);

            // Media Type - allows Apple software to recognise as a movie
            appleTags.SetData("stik", new TagLib.ByteVector(_stikMovie), (uint) TagLib.Mpeg4.AppleDataBox.FlagType.ContainsData);
        }
    }

    private const byte _stikMovie = 9;

    public override string GetFileName(AutoTagConfig config)
    {
        return _renameRegex.Replace(config.MovieRenamePattern, (m) =>
        {
            switch (m.Groups["num"].Value)
            {
                case "1": return Title;
                case "2": return Date.HasValue ? FormatRenameNumber(m, Date.Value.Year) : "";
                default: return m.Value;
            }
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
            return Title;
        }
    }
}