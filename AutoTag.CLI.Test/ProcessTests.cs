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

            using (new AssertionScope())
            {
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