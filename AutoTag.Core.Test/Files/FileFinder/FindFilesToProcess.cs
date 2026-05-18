using AutoTag.Core.Config;
using AutoTag.Core.Files.Parsing;
using AutoTag.Core.Test.Helpers;

namespace AutoTag.Core.Test.Files.FileFinder;

public class FindFilesToProcess
{
    [Fact]
    public void Should_FindCommonVideoContainers_AndOnlyTagKnownSafeFormats()
    {
        var (fs, root) = new MockFileSystemBuilder()
            .WithFile("episode.AVI")
            .WithFile("movie.mkv")
            .WithFile("clip.mov")
            .WithFile("notes.txt")
            .WithDirectory("Nested", d => d.WithFile("nested.mp4"))
            .Build();

        var finder = new Core.Files.FileFinder(
            new AutoTagConfig { RenameSubtitles = false },
            fs,
            new Mock<IUserInterface>().Object,
            new Mock<IFileNameParser>().Object
        );

        var result = finder.FindFilesToProcess([root]);

        result.Should().ContainSingle(file => file.Path.EndsWith("episode.AVI") && !file.Taggable);
        result.Should().ContainSingle(file => file.Path.EndsWith("movie.mkv") && file.Taggable);
        result.Should().ContainSingle(file => file.Path.EndsWith("clip.mov") && !file.Taggable);
        result.Should().ContainSingle(file => file.Path.EndsWith("nested.mp4") && file.Taggable);
        result.Should().NotContain(file => file.Path.EndsWith("notes.txt"));
    }

    [Fact]
    public void Should_GroupVideoAndSubtitlesWithSameParsedDetails()
    {
        var (fs, root) = new MockFileSystemBuilder()
            .WithFile("Title S01E02.mkv")
            .WithFile("Title S01E02 ENG.ass")
            .WithFile("Title S01E02.ass")
            .WithDirectory("Movie", d => d.WithFile("Title.mp4").WithFile("Title.srt"))
            .WithFile("Unknown1.mp4")
            .WithFile("Unknown2.srt")
            .Build();

        var mockParser = new Mock<IFileNameParser>();
        mockParser.Setup(m => m.ParseFileName(It.Is<string>(s => s.Contains("Title S01E02"))))
            .Returns((new ParsedTVFileName("Title", null, 1, 2, null, null), new ParsedMovieFileName("Title", null)));

        mockParser.Setup(m => m.ParseFileName(It.Is<string>(s => s.Contains("Title."))))
            .Returns((null, new ParsedMovieFileName("Title", null)));

        mockParser.Setup(m => m.ParseFileName(It.Is<string>(s => s.Contains("Unknown"))))
            .Returns((null, null));

        var finder = new Core.Files.FileFinder(
            new AutoTagConfig { RenameSubtitles = true },
            fs,
            new Mock<IUserInterface>().Object,
            mockParser.Object
        );

        var result = finder.FindFilesToProcess([root]);

        result.Should().ContainSingle(f => f.Path.EndsWith("Title S01E02.mkv")
            && f.SubtitlePaths.Any(s => s.EndsWith("Title S01E02 ENG.ass"))
            && f.SubtitlePaths.Any(s => s.EndsWith("Title S01E02.ass")));

        result.Should().ContainSingle(f => f.Path.EndsWith("Title.mp4")
            && f.SubtitlePaths.Any(s => s.EndsWith("Title.srt")));

        result.Should().ContainSingle(f => f.Path.EndsWith("Unknown1.mp4") && f.SubtitlePaths.Count == 0);
        result.Should().ContainSingle(f => f.Path.EndsWith("Unknown2.srt") && f.SubtitlePaths.Count == 0);
    }
}