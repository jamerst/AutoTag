using AutoTag.Core.Config;
using AutoTag.Core.Files;

namespace AutoTag.Core.Test.Files.FileFinder;

public class FindFilesToProcess
{
    [Fact]
    public void Should_FindCommonVideoContainers_AndOnlyTagKnownSafeFormats()
    {
        var tempDirectory = Directory.CreateTempSubdirectory();

        try
        {
            File.WriteAllText(Path.Combine(tempDirectory.FullName, "episode.AVI"), string.Empty);
            File.WriteAllText(Path.Combine(tempDirectory.FullName, "movie.mkv"), string.Empty);
            File.WriteAllText(Path.Combine(tempDirectory.FullName, "clip.mov"), string.Empty);
            File.WriteAllText(Path.Combine(tempDirectory.FullName, "notes.txt"), string.Empty);
            var nestedDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.FullName, "Nested"));
            File.WriteAllText(Path.Combine(nestedDirectory.FullName, "nested.mp4"), string.Empty);

            var finder = new AutoTag.Core.Files.FileFinder(
                new AutoTagConfig { RenameSubtitles = false },
                new AutoTag.Core.Files.FileSystem(),
                new Mock<IUserInterface>().Object
            );

            var result = finder.FindFilesToProcess([tempDirectory]);

            result.Should().ContainSingle(file => file.Path.EndsWith("episode.AVI") && !file.Taggable);
            result.Should().ContainSingle(file => file.Path.EndsWith("movie.mkv") && file.Taggable);
            result.Should().ContainSingle(file => file.Path.EndsWith("clip.mov") && !file.Taggable);
            result.Should().ContainSingle(file => file.Path.EndsWith("nested.mp4") && file.RootPath == tempDirectory.FullName);
            result.Should().NotContain(file => file.Path.EndsWith("notes.txt"));
            result.Should().OnlyContain(file => file.RootPath == tempDirectory.FullName);
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Should_Associate_Ass_Subtitle_With_Matching_Video_When_RenameSubtitles_Enabled()
    {
        var tempDirectory = Directory.CreateTempSubdirectory();

        try
        {
            var videoPath = Path.Combine(tempDirectory.FullName, "show.mkv");
            var subtitlePath = Path.Combine(tempDirectory.FullName, "show.ass");
            File.WriteAllText(videoPath, string.Empty);
            File.WriteAllText(subtitlePath, string.Empty);

            var finder = new AutoTag.Core.Files.FileFinder(
                new AutoTagConfig { RenameSubtitles = true },
                new AutoTag.Core.Files.FileSystem(),
                new Mock<IUserInterface>().Object
            );

            var result = finder.FindFilesToProcess([tempDirectory]);

            result.Should().ContainSingle(file => file.Path == videoPath && file.SubtitlePath == subtitlePath);
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Should_Associate_Loose_Subtitles_With_Matching_TV_Episodes_When_RenameSubtitles_Enabled()
    {
        var tempDirectory = Directory.CreateTempSubdirectory();

        try
        {
            var seriesDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.FullName, "Koakuma Kanojo the Animation"));
            var seasonDirectory = Directory.CreateDirectory(Path.Combine(seriesDirectory.FullName, "Season 01"));
            var subtitleDirectory = Directory.CreateDirectory(Path.Combine(seriesDirectory.FullName, "Eng"));

            var videoPath = Path.Combine(seasonDirectory.FullName, "Koakuma Kanojo the Animation - 1x01 - So Sticky and Covered in Juice.mkv");
            var subtitlePath1 = Path.Combine(subtitleDirectory.FullName, "[Shinkiro-raw] Koakuma Kanojo The Animation - 01 [7AD743D9].eng [EROBEAT_LQ].ass");
            var subtitlePath2 = Path.Combine(subtitleDirectory.FullName, "[Shinkiro-raw] Koakuma Kanojo The Animation - 01 [7AD743D9].eng [SubDESU-H].ass");
            File.WriteAllText(videoPath, string.Empty);
            File.WriteAllText(subtitlePath1, string.Empty);
            File.WriteAllText(subtitlePath2, string.Empty);

            var finder = new AutoTag.Core.Files.FileFinder(
                new AutoTagConfig { RenameSubtitles = true },
                new AutoTag.Core.Files.FileSystem(),
                new Mock<IUserInterface>().Object
            );

            var result = finder.FindFilesToProcess([tempDirectory]);

            var video = result.Should().ContainSingle(file => file.Path == videoPath).Subject;
            video.SubtitlePaths.Should().BeEquivalentTo([subtitlePath1, subtitlePath2]);
            result.Should().NotContain(file => file.Path == subtitlePath1 || file.Path == subtitlePath2);
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }
}
