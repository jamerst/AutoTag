using AutoTag.Core.Config;
using AutoTag.Core.Files;
using TagLib;
using TagLib.Mpeg4;
using File = TagLib.File;
using Tag = TagLib.Matroska.Tag;

namespace AutoTag.Core.TV;

public class TVFileMetadata : FileMetadata
{
    private const byte StikTVShow = 10;

    public required string SeriesName { get; init; }

    public int? Year { get; init; }

    public int Season { get; init; }

    public int Episode { get; init; }

    public int? EndEpisode { get; init; }

    public int SeasonEpisodes { get; init; }

    public int? Part { get; init; }

    public override void WriteToFile(File file, AutoTagConfig config, IUserInterface ui)
    {
        base.WriteToFile(file, config, ui);

        if ((file.TagTypes & TagTypes.Matroska) == TagTypes.Matroska)
        {
            var custom = (Tag)file.GetTag(TagTypes.Matroska);
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

        file.Tag.Disc = (uint)Season;
        file.Tag.Track = (uint)Episode;
        file.Tag.TrackCount = (uint)SeasonEpisodes;

        // set extra tags because Apple is stupid and uses different tags for some reason
        // for a list of tags see https://kdenlive.org/en/project/adding-meta-data-to-mp4-video/
        if (config.AppleTagging && (file.TagTypes & TagTypes.Apple) == TagTypes.Apple)
        {
            var appleTags = (AppleTag)file.GetTag(TagTypes.Apple);

            // Media Type - allows Apple software to recognise as a TV show
            // for a list of values see http://www.zoyinc.com/?p=1004
            appleTags.SetData("stik", new ByteVector(StikTVShow), (uint)AppleDataBox.FlagType.ContainsData);

            // Series
            appleTags.SetText("tvsh", SeriesName);

            if (Season is >= byte.MinValue and <= byte.MaxValue)
            {
                // Season number
                appleTags.SetData("tvsn", new ByteVector((byte)Season), (uint)AppleDataBox.FlagType.ContainsData);
            }
            else
            {
                ui.SetStatus("Warning: cannot add Apple tag for season number - value out of range",
                    MessageType.Warning);
            }

            if (Episode is >= byte.MinValue and <= byte.MaxValue)
            {
                // Episode number
                appleTags.SetData("tves", new ByteVector((byte)Episode), (uint)AppleDataBox.FlagType.ContainsData);
            }
            else
            {
                ui.SetStatus("Warning: cannot add Apple tag for episode number - value out of range",
                    MessageType.Warning);
            }

            // Sort name - allows older Apple software to sort correctly (sorts by title instead of season and episode on older devices)
            appleTags.SetText("sonm", $"S{Season:00}E{Episode:00}");
        }
    }

    public override string GetRenamePattern(AutoTagConfig config) => config.TVRenamePattern;

    public override IEnumerable<IFileNameField> GetRenameFields()
    {
        yield return new StringFileNameField("Series", "1", SeriesName);
        yield return new IntegerFileNameField("Season", "2", Season);
        yield return new IntegerFileNameField("Episode", "3", Episode);
        yield return new StringFileNameField("Title", "4", Title);
        yield return new IntegerFileNameField("Year", null, Year);
        yield return new IntegerFileNameField("EndEpisode", null, EndEpisode);
        yield return new IntegerFileNameField("Part", null, Part);
    }

    public override string ToString()
        => $"{SeriesName} S{Season:00}E{Episode:00} ({Title})";
}