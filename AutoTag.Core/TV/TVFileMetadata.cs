namespace AutoTag.Core.TV;

public class TVFileMetadata : FileMetadata
{
    public string SeriesName = null!;
    public int Season;
    public int Episode;
    public int SeasonEpisodes;

    public override void WriteToFile(TagLib.File file, AutoTagConfig config, IUserInterface ui)
    {
        base.WriteToFile(file, config, ui);

        if ((file.TagTypes & TagLib.TagTypes.Matroska) == TagLib.TagTypes.Matroska)
        {
            var custom = (TagLib.Matroska.Tag)file.GetTag(TagLib.TagTypes.Matroska);
            // workaround for https://github.com/mono/taglib-sharp/issues/263 - Tag.Album property writes to TITLE tag instead of ALBUM
            // how has this still not been fixed??
            custom.Set("ALBUM", null, SeriesName);

            if (config.ExtendedTagging)
            {
                custom.Set("TMDB", null, $"tv/{Id}");
            }
        }
        else
        {
            file.Tag.Album = SeriesName;
        }

        file.Tag.Disc = (uint) Season;
        file.Tag.Track = (uint) Episode;
        file.Tag.TrackCount = (uint) SeasonEpisodes;

        // set extra tags because Apple is stupid and uses different tags for some reason
        // for a list of tags see https://kdenlive.org/en/project/adding-meta-data-to-mp4-video/
        if (config.AppleTagging && (file.TagTypes & TagLib.TagTypes.Apple) == TagLib.TagTypes.Apple)
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
                ui.SetStatus($"Warning: cannot add Apple tag for season number - value out of range", MessageType.Warning);
            }

            if (Episode >= byte.MinValue && Episode <= byte.MaxValue)
            {
                // Episode number
                appleTags.SetData("tves", new TagLib.ByteVector((byte) Episode), (uint) TagLib.Mpeg4.AppleDataBox.FlagType.ContainsData);
            }
            else
            {
                ui.SetStatus($"Warning: cannot add Apple tag for episode number - value out of range", MessageType.Warning);
            }

            // Sort name - allows older Apple software to sort correctly (sorts by title instead of season and episode on older devices)
            appleTags.SetText("sonm", $"S{Season:00}E{Episode:00}");
        }
    }

    private const byte _stikTVShow = 10;

    public override string GetFileName(AutoTagConfig config)
    {
        return _renameRegex.Replace(config.TVRenamePattern, (m) =>
        {
            return m.Groups["num"].Value switch
            {
                "1" => SeriesName,
                "2" => FormatRenameNumber(m, Season),
                "3" => FormatRenameNumber(m, Episode),
                "4" => Title!,
                _ => m.Value
            };
        });
    }

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(Title))
        {
            return $"{SeriesName} S{Season:00}E{Episode:00} ({Title})";
        }
        else
        {
            return $"{SeriesName} S{Season:00}E{Episode:00}";
        }
    }
}