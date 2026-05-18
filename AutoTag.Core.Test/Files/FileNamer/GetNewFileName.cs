using AutoTag.Core.Config;
using AutoTag.Core.Files;
using AutoTag.Core.Movie;
using AutoTag.Core.TV;

namespace AutoTag.Core.Test.Files.FileNamer;

public class GetNewFileName
{
    [Fact]
    public void Should_ReplaceLegacySpecifiersInTVPattern_WithoutFormats()
        => TestGetNewFileName(
            new AutoTagConfig
            {
                TVRenamePattern = "%1 S%2E%3 %4"
            },
            new TVFileMetadata
            {
                SeriesName = "Show Name",
                Season = 3,
                Episode = 15,
                Title = "Episode Title"
            },
            "Show Name S3E15 Episode Title",
            false
        );

    [Fact]
    public void Should_ReplaceLegacySpecifiersInTVPattern_WithFormats()
        => TestGetNewFileName(
            new AutoTagConfig
            {
                TVRenamePattern = "%1 S%2:00E%3:000 %4"
            },
            new TVFileMetadata
            {
                SeriesName = "Show Name",
                Season = 3,
                Episode = 15,
                Title = "Episode Title"
            },
            "Show Name S03E015 Episode Title",
            false
        );

    [Fact]
    public void Should_ReplaceSpecifiersInTVPattern_WithoutFormats()
        => TestGetNewFileName(
            new AutoTagConfig
            {
                TVRenamePattern = "{Series} S{Season}E{Episode} {Title}"
            },
            new TVFileMetadata
            {
                SeriesName = "Show Name",
                Season = 12,
                Episode = 1,
                Title = "Episode Title"
            },
            "Show Name S12E1 Episode Title",
            false
        );

    [Fact]
    public void Should_ReplaceSpecifiersInTVPattern_WithFormats()
        => TestGetNewFileName(
            new AutoTagConfig
            {
                TVRenamePattern = "{Series} {Season:S00|Specials}{Episode:E000} {Title}"
            },
            new TVFileMetadata
            {
                SeriesName = "Show Name",
                Season = 5,
                Episode = 4,
                Title = "Episode Title"
            },
            "Show Name S05E004 Episode Title",
            false
        );

    [Fact]
    public void Should_ReplaceLegacySpecifiersInMoviePattern_WithoutFormats()
        => TestGetNewFileName(
            new AutoTagConfig
            {
                MovieRenamePattern = "%1 (%2)"
            },
            new MovieFileMetadata
            {
                Title = "Movie Name",
                Date = new DateTime(1999, 06, 07)
            },
            "Movie Name (1999)",
            false
        );

    [Fact]
    public void Should_ReplaceLegacySpecifiersInMoviePattern_WithFormats()
        => TestGetNewFileName(
            new AutoTagConfig
            {
                MovieRenamePattern = "%1 (%2:00000)"
            },
            new MovieFileMetadata
            {
                Title = "Movie Name",
                Date = new DateTime(1999, 06, 07)
            },
            "Movie Name (01999)",
            false
        );

    [Fact]
    public void Should_ReplaceSpecifiersInMoviePattern_WithoutFormats()
        => TestGetNewFileName(
            new AutoTagConfig
            {
                MovieRenamePattern = "{Title} ({Year})"
            },
            new MovieFileMetadata
            {
                Title = "Movie Name",
                Date = new DateTime(1999, 06, 07)
            },
            "Movie Name (1999)",
            false
        );

    [Fact]
    public void Should_ReplaceSpecifiersInMoviePattern_WithFormats()
        => TestGetNewFileName(
            new AutoTagConfig
            {
                MovieRenamePattern = "{Title} {Year:(0000)}"
            },
            new MovieFileMetadata
            {
                Title = "Movie Name",
                Date = new DateTime(1999, 06, 07)
            },
            "Movie Name (1999)",
            false
        );

    [Fact]
    public void Should_UseAlternativeValue_WhenValueIs0()
        => TestGetNewFileName(
            new AutoTagConfig
            {
                TVRenamePattern = "{Series} {Season:S00|Specials }{Episode:E000} {Title}"
            },
            new TVFileMetadata
            {
                SeriesName = "Show Name",
                Season = 0,
                Episode = 4,
                Title = "Episode Title"
            },
            "Show Name Specials E004 Episode Title",
            false
        );

    [Fact]
    public void Should_UseAlternative_WhenValueIsNull()
        => TestGetNewFileName(
            new AutoTagConfig
            {
                MovieRenamePattern = "{Title}{Year: 0000|}" // empty alternative value
            },
            new MovieFileMetadata
            {
                Title = "Movie Name",
                Date = null
            },
            "Movie Name",
            false
        );

    [Fact]
    public void Should_ApplyFileNameReplacesToFieldValues()
        => TestGetNewFileName(
            new AutoTagConfig
            {
                TVRenamePattern = "Test {Series} {Season}x{Episode:00} {Title}",
                FileNameReplaces =
                [
                    new FileNameReplace("replace1", "replacement1"),
                    new FileNameReplace("Test ", "")
                ]
            },
            new TVFileMetadata
            {
                SeriesName = "Name of replace1 show",
                Season = 0,
                Episode = 4,
                Title = "Test Episode Title"
            },
            "Test Name of replacement1 show 0x04 Episode Title",
            false
        );

    [Fact]
    public void Should_RemoveInvalidFileNameCharactersFromFieldValues()
        => TestGetNewFileName(
            new AutoTagConfig
            {
                TVRenamePattern = "/TV/{Series}/{Series} {Season:S00}{Episode:E00} {Title}"
            },
            new TVFileMetadata
            {
                SeriesName = "Show/Name",
                Season = 8,
                Episode = 2,
                Title = "Episode Title/"
            },
            "/TV/ShowName/ShowName S08E02 Episode Title",
            true
        );

    [Fact]
    public void Should_RemoveInvalidNTFSFileNameCharactersFromFieldValues()
        => TestGetNewFileName(
            new AutoTagConfig
            {
                TVRenamePattern = "/TV/{Series}/{Series} {Season:S00}{Episode:E00} {Title}",
                WindowsSafe = true
            },
            new TVFileMetadata
            {
                SeriesName = "ShowName",
                Season = 8,
                Episode = 2,
                Title = "Episode Title/: Test"
            },
            "/TV/ShowName/ShowName S08E02 Episode Title Test",
            true
        );

    private static void TestGetNewFileName(AutoTagConfig config, FileMetadata metadata, string expectedResult,
        bool expectedReplacedInvalid)
    {
        var namer = new Core.Files.FileNamer(config);

        var (result, replacedInvalid) = namer.GetNewFileName(metadata);

        result.Should().Be(expectedResult);
        replacedInvalid.Should().Be(expectedReplacedInvalid);
    }
}