namespace autotag.Core.TV;

public class TVFileMetadata : FileMetadata
{
    public string SeriesName = null!;
    public int Season;
    public int Episode;
    public int SeasonEpisodes;

    public override void WriteToFile(TagLib.File file, AutoTagConfig config, Action<string, MessageType> setStatus)
    {
        base.WriteToFile(file, config, setStatus);

        if (config.ExtendedTagging && file.MimeType == "video/x-matroska")
        {
            var custom = (TagLib.Matroska.Tag) file.GetTag(TagLib.TagTypes.Matroska);
            custom.Set("TMDB", "", $"tv/{Id}");
        }

        file.Tag.Album = SeriesName;
        file.Tag.Disc = (uint) Season;
        file.Tag.Track = (uint) Episode;
        file.Tag.TrackCount = (uint) SeasonEpisodes;

        // set extra tags because Apple is stupid and uses different tags for some reason
        // for a list of tags see https://kdenlive.org/en/project/adding-meta-data-to-mp4-video/
        if (config.AppleTagging && file.MimeType.EndsWith("/mp4"))
        {
            var appleTags = (TagLib.Mpeg4.AppleTag) file.GetTag(TagLib.TagTypes.Apple);

            // Media Type - allows Apple software to recognise as a TV show
            // for a list of values see http://www.zoyinc.com/?p=1004
            appleTags.SetData("stik", new TagLib.ByteVector(_stikTVShow), (uint) TagLib.Mpeg4.AppleDataBox.FlagType.ContainsData);

            // Series
            appleTags.SetText("tvsh", SeriesName);

            if (Season >= byte.MinValue && Season <= byte.MaxValue)
            {
                // Season number
                appleTags.SetData("tvsn", new TagLib.ByteVector((byte) Season), (uint) TagLib.Mpeg4.AppleDataBox.FlagType.ContainsData);
            }
            else
            {
                setStatus($"Warning: cannot add Apple tag for season number - value out of range", MessageType.Warning);
            }

            if (Episode >= byte.MinValue && Episode <= byte.MaxValue)
            {
                // Episode number
                appleTags.SetData("tves", new TagLib.ByteVector((byte) Episode), (uint) TagLib.Mpeg4.AppleDataBox.FlagType.ContainsData);
            }
            else
            {
                setStatus($"Warning: cannot add Apple tag for episode number - value out of range", MessageType.Warning);
            }
        }
    }

    private const byte _stikTVShow = 10;

    public override string GetFileName(AutoTagConfig config)
    {
        return _renameRegex.Replace(config.TVRenamePattern, (m) =>
        {
            switch (m.Groups["num"].Value)
            {
                case "1": return SeriesName;
                case "2": return FormatRenameNumber(m, Season);
                case "3": return FormatRenameNumber(m, Episode);
                case "4": return Title;
                default: return m.Value;
            }
        });
    }

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(Title))
        {
            return $"{SeriesName} S{Season.ToString("00")}E{Episode.ToString("00")} ({Title})";
        }
        else
        {
            return $"{SeriesName} S{Season.ToString("00")}E{Episode.ToString("00")}";
        }
    }
}