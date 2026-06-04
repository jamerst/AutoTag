using AutoTag.Core.Config;
using AutoTag.Core.Files.Parsing;
using AutoTag.Core.Test.Helpers;

namespace AutoTag.Core.Test.Files.TVFileNameParser;

public class TryParse
{
    private static Core.Files.Parsing.TVFileNameParser GetInstance(AutoTagConfig? config = null)
        => new(config.OrDefaultMock());

    [Theory]
    [InlineData("Fringe 3x03.mkv", "Fringe", null, 3, 3, null, null)]
    [InlineData("Silo S01E10.mkv", "Silo", null, 1, 10, null, null)]
    [InlineData("Inside.No.9.S09E09.mkv", "Inside No 9", null, 9, 9, null,
        null)] // dot separated and number in series name
    [InlineData("the_last_of_us_s01e07.mp4", "The Last of Us", null, 1, 7, null,
        null)] // underscore separated + lowercase
    [InlineData("Alias.S02E01.The.Enemy.Walks.In.1080p.AMZN.WEB-DL.DDP5.1.H.264-LycanHD.mkv", "Alias", null, 2,
        1, null, null)] // scene tags
    [InlineData("Warehouse 13 0x01 Episode Title.mp4", "Warehouse 13", null, 0, 1, null, null)] // season 0
    [InlineData("Doctor Who (2005) - 2x13 - Doomsday (2).mkv", "Doctor Who", 2005, 2, 13, null,
        null)] // symbols in series name
    [InlineData("Serial Experiments Lain E12 Landscape 1080p BluRay FLAC 2.0 x264-Chotab.mkv",
        "Serial Experiments Lain", null, null, 12, null, null)] // absolute episode
    [InlineData("Fallout.2024.S02E06.1080p.WEB.h264-ETHEL[EZTVx.to].mkv", "Fallout", 2024, 2, 6, null, null)] // year
    [InlineData("Absolute.Episode.Show.001.mkv", "Absolute Episode Show", null, null, 1, null,
        null)] // absolute numbering with no delimiter
    [InlineData("Beavis and Butt-Head - 2x10-11 - Way Down Mexico Way.mkv", "Beavis and Butt Head", null, 2, 10, 11,
        null)] // multi-episode file
    [InlineData("Test.S02E08-E09.mkv", "Test", null, 2, 8, 9, null)] // multi-episode file
    [InlineData("Lost - 1x24 - Exodus (2) - pt1.mkv", "Lost", null, 1, 24, null, 1)] // part file
    [InlineData("Absolute.Episode.Show.002-003.pt4.mkv", "Absolute Episode Show", null, null, 2, 3,
        4)] // absolute numbering with end episode and part
    [InlineData("[Group] Series Dublado (35).AVI", "Series", null, null, 35, null, null)]
    public void Should_ParseCommonNamingFormats(string fileName, string seriesName, int? year, int? season, int episode,
        int? endEpisode, int? part)
    {
        var parser = GetInstance();

        var success = parser.TryParse(fileName, out var result);

        success.Should().BeTrue();
        result.Should().BeEquivalentTo(
            new ParsedTVFileName(seriesName, year, season, episode, endEpisode, part),
            o => o.Using<string>(StringComparer.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void Should_RemoveSceneTagsAndLanguageTermsFromTitle()
    {
        var parser = GetInstance();

        var success = parser.TryParse("[Scene] [Tag] Subbed Legendado ENG Show Name S06E27 [Tag].mp4", out var result);

        success.Should().BeTrue();
        result.Should().BeEquivalentTo(
            new ParsedTVFileName("Show Name", null, 6, 27, null, null),
            o => o.Using<string>(StringComparer.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void Should_ParseFromFullPath_When_ParsePatternProvided()
    {
        var config = new AutoTagConfig
        {
            ParsePattern = @".*/(?<SeriesName>.+)/Season (?<Season>\d+)/S\d+E(?<Episode>\d+)"
        };

        var parser = GetInstance(config);

        var success = parser.TryParse("/test/a/b/Series Name/Season 2/S02E05 Title.mkv", out var result);

        success.Should().BeTrue();
        result.Should().BeEquivalentTo(new ParsedTVFileName("Series Name", null, 2, 5, null, null));
    }

    [Theory]
    [InlineData("S01E05.mkv")]
    [InlineData("28 Years Later 2025 1080p MA WEB-DL.mkv")]
    [InlineData("25.mp4")]
    public void Should_ReturnFalse_When_PartMissingFromFileName(string fileName)
    {
        var parser = GetInstance();

        var success = parser.TryParse(fileName, out _);

        success.Should().BeFalse();
    }

    [Fact]
    public void Should_ReturnFalse_When_ParsePatternDoesNotMatch()
    {
        var config = new AutoTagConfig
        {
            ParsePattern = @".*/(?<SeriesName>.+)/Season (?<Season>\d+)/S\d+E(?<Episode>\d+)"
        };

        var parser = GetInstance(config);

        var success = parser.TryParse("/test/a/b/Series Name/S02/Series Name 2x05 Title.mkv", out _);
        success.Should().BeFalse();
    }
}