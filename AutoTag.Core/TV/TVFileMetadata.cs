using AutoTag.Core.Config;
using AutoTag.Core.Files;
using AutoTag.Core.Tagging;

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

    public override Tags GetTags(AutoTagConfig config)
    {
        var tags = base.GetTags(config);

        tags.Album = SeriesName;
        tags.Disc = Season;
        tags.Track = Episode;
        tags.TrackCount = SeasonEpisodes;

        if (config.ExtendedTagging)
        {
            tags.Custom = [("TMDB", $"tv/{Id}")];
        }

        tags.AppleMediaType = StikTVShow;

        return tags;
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