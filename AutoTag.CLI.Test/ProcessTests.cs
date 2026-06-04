using AutoTag.CLI.Test.Helpers;
using AwesomeAssertions.Execution;
using TagLib;
using File = System.IO.File;
using TagLibFile = TagLib.File;
using Tag = TagLib.Matroska.Tag;

namespace AutoTag.CLI.Test;

public class ProcessTests(CLIFixture cli) : CLITestBase
{
    [Fact]
    public async Task Should_ProcessFilesWithRenamePattern()
    {
        FileSystem
            .CreateDirectory("Downloads",
                d => d
                    .CreateDirectory("house.of.the.dragon.s02",
                        d2 => d2.CreateFile("house.of.the.dragon.s02e01.mkv")
                            .CreateFile("house.of.the.dragon.s02e02.mkv")
                    )
                    .CreateDirectory("The Testaments S01E10",
                        d2 => d2.CreateFile("The Testaments S01E10.mp4")
                            .CreateFile("The Testaments S01E10.srt")
                    )
            )
            .CreateDirectory("Movies", d => d
                .CreateFile("Interstellar.2014.avi")
                .CreateFile("Star Wars (1977).mkv")
            );

        var (_, exitCode) = await cli.ExecuteAsync(
            FileSystem.GetPath("Downloads"),
            FileSystem.GetPath("Movies", "Interstellar.2014.avi"),
            FileSystem.GetPath("Movies", "Star Wars (1977).mkv"),
            "--tv-pattern", "{Series} {Season:S00}{Episode:E00}",
            "--movie-pattern", "{Title} ({Year})",
            "--rename-subs"
        );

        exitCode.Should().Be(0);

        AssertFile(
            FileSystem.GetPath("Downloads", "house.of.the.dragon.s02", "house.of.the.dragon.s02e01.mkv"),
            FileSystem.GetPath("Downloads", "house.of.the.dragon.s02", "House of the Dragon S02E01.mkv"),
            f =>
            {
                f.Title.Should().Be("A Son for a Son");
                f.Description.Should().NotBeEmpty();
                f.Genres.Should().NotBeEmpty();
                f.Album.Should().Be("House of the Dragon");
                f.Disc.Should().Be(2);
                f.Track.Should().Be(1);
                f.TrackCount.Should().Be(8);
            }
        );

        AssertFile(
            FileSystem.GetPath("Downloads", "house.of.the.dragon.s02", "house.of.the.dragon.s02e02.mkv"),
            FileSystem.GetPath("Downloads", "house.of.the.dragon.s02", "House of the Dragon S02E02.mkv"),
            f =>
            {
                f.Title.Should().Be("Rhaenyra the Cruel");
                f.Description.Should().NotBeEmpty();
                f.Genres.Should().NotBeEmpty();
                f.Album.Should().Be("House of the Dragon");
                f.Disc.Should().Be(2);
                f.Track.Should().Be(2);
                f.TrackCount.Should().Be(8);
            }
        );

        AssertFile(
            FileSystem.GetPath("Downloads", "The Testaments S01E10", "The Testaments S01E10.mp4"),
            FileSystem.GetPath("Downloads", "The Testaments S01E10", "The Testaments S01E10.mp4"),
            f =>
            {
                f.Title.Should().Be("Secateurs");
                f.Description.Should().NotBeEmpty();
                f.Genres.Should().NotBeEmpty();
                f.Album.Should().Be("The Testaments");
                f.Disc.Should().Be(1);
                f.Track.Should().Be(10);
                f.TrackCount.Should().Be(10);
            }
        );

        AssertFile(
            FileSystem.GetPath("Downloads", "The Testaments S01E10", "The Testaments S01E10.srt"),
            FileSystem.GetPath("Downloads", "The Testaments S01E10", "The Testaments S01E10.srt")
        );

        AssertFile(
            FileSystem.GetPath("Movies", "Interstellar.2014.avi"),
            FileSystem.GetPath("Movies", "Interstellar (2014).avi")
        );

        AssertFile(
            FileSystem.GetPath("Movies", "Star Wars (1977).mkv"),
            FileSystem.GetPath("Movies", "Star Wars (1977).mkv"),
            f =>
            {
                f.Title.Should().Be("Star Wars");
                f.Description.Should().NotBeEmpty();
                f.Genres.Should().NotBeEmpty();
                f.Year.Should().Be(1977);
            }
        );
    }

    [Fact]
    public async Task Should_ProcessFilesWithAbsoluteRenamePattern()
    {
        FileSystem
            .CreateDirectory("Downloads",
                d => d
                    .CreateDirectory("house.of.the.dragon.s02",
                        d2 => d2.CreateFile("house.of.the.dragon.s02e01.mkv")
                            .CreateFile("house.of.the.dragon.s02e02.mkv")
                    )
                    .CreateDirectory("The Testaments S01E10",
                        d2 => d2.CreateFile("The Testaments S01E10.mp4")
                            .CreateFile("The Testaments S01E10.srt")
                    )
                    .CreateDirectory("Doctor Who",
                        d2 => d2.CreateFile("Doctor.Who.2005.S00E02.mkv")
                            .CreateFile("Doctor.Who.2005.S01E01.pt1.mkv")
                            .CreateFile("Doctor.Who.2005.S01E02-03.mkv")
                            .CreateFile("Doctor.Who.2005.S01.scene.nfo")
                    )
                    .CreateDirectory("lotr",
                        d2 => d2.CreateFile("The Lord of the Rings The Fellowship of the Ring.mp4"))
                    .CreateDirectory("Empty", _ => { })
            )
            .CreateDirectory("Movies", d => d
                .CreateFile("Interstellar.2014.avi")
                .CreateFile("Star Wars (1977).mkv")
            );

        var (_, exitCode) = await cli.ExecuteAsync(
            FileSystem.GetPath("Downloads"),
            FileSystem.GetPath("Movies", "Interstellar.2014.avi"),
            FileSystem.GetPath("Movies", "Star Wars (1977).mkv"),
            "--tv-pattern",
            FileSystem.GetPath(
                "TV",
                "{Series}{Year: (0)|}",
                "{Season:Season 0|Specials}",
                "{Series}{Year: (0)|} {Season:S00}{Episode:E00}{EndEpisode:-00|}{Part: pt0|}"
            ),
            "--movie-pattern", FileSystem.GetPath("Movies", "{Title} ({Year})"),
            "--rename-subs",
            "--remove-empty-folders",
            "--windows-safe"
        );

        exitCode.Should().Be(0);

        // isn't empty so should remain
        Directory.Exists(FileSystem.GetPath("Downloads", "Doctor Who")).Should().BeTrue();

        Directory.Exists(FileSystem.GetPath("Downloads", "house.of.the.dragon.s02")).Should().BeFalse();
        Directory.Exists(FileSystem.GetPath("Downloads", "The Testaments S01E10")).Should().BeFalse();
        Directory.Exists(FileSystem.GetPath("Downloads", "lotr")).Should().BeFalse();

        AssertFile(
            FileSystem.GetPath("Downloads", "house.of.the.dragon.s02", "house.of.the.dragon.s02e01.mkv"),
            FileSystem.GetPath("TV", "House of the Dragon", "Season 2", "House of the Dragon S02E01.mkv"),
            f =>
            {
                f.Title.Should().Be("A Son for a Son");
                f.Description.Should().NotBeEmpty();
                f.Genres.Should().NotBeEmpty();
                f.Album.Should().Be("House of the Dragon");
                f.Disc.Should().Be(2);
                f.Track.Should().Be(1);
                f.TrackCount.Should().Be(8);
            }
        );

        AssertFile(
            FileSystem.GetPath("Downloads", "house.of.the.dragon.s02", "house.of.the.dragon.s02e02.mkv"),
            FileSystem.GetPath("TV", "House of the Dragon", "Season 2", "House of the Dragon S02E02.mkv"),
            f =>
            {
                f.Title.Should().Be("Rhaenyra the Cruel");
                f.Description.Should().NotBeEmpty();
                f.Genres.Should().NotBeEmpty();
                f.Album.Should().Be("House of the Dragon");
                f.Disc.Should().Be(2);
                f.Track.Should().Be(2);
                f.TrackCount.Should().Be(8);
            }
        );

        AssertFile(
            FileSystem.GetPath("Downloads", "The Testaments S01E10", "The Testaments S01E10.mp4"),
            FileSystem.GetPath("TV", "The Testaments", "Season 1", "The Testaments S01E10.mp4"),
            f =>
            {
                f.Title.Should().Be("Secateurs");
                f.Description.Should().NotBeEmpty();
                f.Genres.Should().NotBeEmpty();
                f.Album.Should().Be("The Testaments");
                f.Disc.Should().Be(1);
                f.Track.Should().Be(10);
                f.TrackCount.Should().Be(10);
            }
        );

        AssertFile(
            FileSystem.GetPath("Downloads", "The Testaments S01E10", "The Testaments S01E10.srt"),
            FileSystem.GetPath("TV", "The Testaments", "Season 1", "The Testaments S01E10.srt")
        );

        AssertFile(
            FileSystem.GetPath("Downloads", "Doctor Who", "Doctor.Who.2005.S00E02.mkv"),
            FileSystem.GetPath("TV", "Doctor Who (2005)", "Specials", "Doctor Who (2005) S00E02.mkv"),
            f =>
            {
                f.Title.Should().Be("The Christmas Invasion");
                f.Description.Should().NotBeEmpty();
                f.Genres.Should().NotBeEmpty();
                f.Album.Should().Be("Doctor Who");
                f.Disc.Should().Be(0);
                f.Track.Should().Be(2);
                f.TrackCount.Should().Be(199);
            }
        );

        AssertFile(
            FileSystem.GetPath("Downloads", "Doctor Who", "Doctor.Who.2005.S01E01.pt1.mkv"),
            FileSystem.GetPath("TV", "Doctor Who (2005)", "Season 1", "Doctor Who (2005) S01E01 pt1.mkv"),
            f =>
            {
                f.Title.Should().Be("Rose");
                f.Description.Should().NotBeEmpty();
                f.Genres.Should().NotBeEmpty();
                f.Album.Should().Be("Doctor Who");
                f.Disc.Should().Be(1);
                f.Track.Should().Be(1);
                f.TrackCount.Should().Be(13);
            }
        );

        AssertFile(
            FileSystem.GetPath("Downloads", "Doctor Who", "Doctor.Who.2005.S01E02-03.mkv"),
            FileSystem.GetPath("TV", "Doctor Who (2005)", "Season 1", "Doctor Who (2005) S01E02-03.mkv"),
            f =>
            {
                f.Title.Should().Be("The End of the World");
                f.Description.Should().NotBeEmpty();
                f.Genres.Should().NotBeEmpty();
                f.Album.Should().Be("Doctor Who");
                f.Disc.Should().Be(1);
                f.Track.Should().Be(2);
                f.TrackCount.Should().Be(13);
            }
        );

        AssertFile(
            FileSystem.GetPath("Downloads", "lotr", "The Lord of the Rings The Fellowship of the Ring.mp4"),
            FileSystem.GetPath("Movies", "The Lord of the Rings The Fellowship of the Ring (2001).mp4"),
            f =>
            {
                f.Title.Should().Be("The Lord of the Rings: The Fellowship of the Ring");
                f.Description.Should().NotBeEmpty();
                f.Genres.Should().NotBeEmpty();
                f.Year.Should().Be(2001);
            }
        );

        AssertFile(
            FileSystem.GetPath("Movies", "Interstellar.2014.avi"),
            FileSystem.GetPath("Movies", "Interstellar (2014).avi")
        );

        AssertFile(
            FileSystem.GetPath("Movies", "Star Wars (1977).mkv"),
            FileSystem.GetPath("Movies", "Star Wars (1977).mkv"),
            f =>
            {
                f.Title.Should().Be("Star Wars");
                f.Description.Should().NotBeEmpty();
                f.Genres.Should().NotBeEmpty();
                f.Year.Should().Be(1977);
            }
        );
    }

    private void AssertFile(string originalPath, string newPath, Action<FileTags>? assertTags = null)
    {
        if (originalPath != newPath)
        {
            File.Exists(originalPath).Should().BeFalse();
        }

        File.Exists(newPath).Should().BeTrue();

        if (assertTags != null && Path.GetExtension(newPath) is ".mkv" or ".mp4")
        {
            using var file = TagLibFile.Create(newPath);

            var tags = new FileTags(
                file.Tag.Title,
                file.Tag.Description,
                file.Tag.Genres,
                file.TagTypes.HasFlag(TagTypes.Matroska)
                    ? ((Tag)file.GetTag(TagTypes.Matroska)).Get("ALBUM")?.FirstOrDefault() ?? ""
                    : file.Tag.Album,
                file.Tag.Disc,
                file.Tag.Track,
                file.Tag.TrackCount,
                file.Tag.Year
            );

            using (new AssertionScope())
            {
                assertTags(tags);
            }
        }
    }

    private record FileTags(
        string Title,
        string Description,
        string[] Genres,
        string Album,
        uint Disc,
        uint Track,
        uint TrackCount,
        uint Year
    );
}