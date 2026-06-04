using AutoTag.Core.Config;
using AutoTag.Core.Files;
using AutoTag.Core.Movie;
using AutoTag.Core.Test.Helpers;
using AutoTag.Core.TV;

namespace AutoTag.Core.Test.Files.FileWriter;

public class WriteAsync
{
    private static Core.Files.FileWriter GetInstance(ICoverArtFetcher? fetcher = null, AutoTagConfig? config = null,
        IFileSystem? fs = null, IUserInterface? ui = null, IFileNamer? namer = null)
        => new(
            fetcher.OrDefaultMock(),
            config.OrDefaultMock(),
            fs.OrDefaultMock(),
            ui.OrDefaultMock(),
            namer ?? new Core.Files.FileNamer(config.OrDefaultMock())
        );

    private static string GetPath(params string[] segments) =>
        Path.Combine([OperatingSystem.IsWindows() ? @"C:\" : "/", ..segments]);

    [Fact]
    public async Task Should_SkipRename_WhenVideoAndSubtitleAreAlreadyCorrectlyNamed()
    {
        var config = new AutoTagConfig
        {
            RenameFiles = true,
            TagFiles = false
        };

        var mockFs = new Mock<IFileSystem>();
        mockFs.Setup(fs => fs.GetDirectoryPath(It.IsAny<string>()))
            .Returns(GetPath());

        var mockUi = new Mock<IUserInterface>();

        var writer = GetInstance(config: config, fs: mockFs.Object, ui: mockUi.Object);
        var taggingFile = new TaggingFile
        {
            Path = GetPath("Movie (2020).mkv"),
            SubtitlePaths = [GetPath("Movie (2020).srt")]
        };

        var metadata = new MovieFileMetadata
        {
            Title = "Movie",
            Date = new DateTime(2020, 1, 1)
        };

        var result = await writer.WriteAsync(taggingFile, metadata);

        result.Should().BeTrue();
        mockFs.Verify(fs => fs.Move(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        mockUi.Verify(ui => ui.SetStatus("Rename skipped - already named correctly", MessageType.Information),
            Times.Once);
    }

    [Fact]
    public async Task Should_NotSkip_WhenSubtitleNameIsWrong()
    {
        var config = new AutoTagConfig
        {
            RenameFiles = true,
            TagFiles = false
        };

        var mockFs = new Mock<IFileSystem>();

        var inPath = GetPath("subtitle.srt");
        var outPath = GetPath("Movie (2020).srt");

        mockFs.Setup(fs => fs.Exists(outPath))
            .Returns(false);
        mockFs.Setup(fs => fs.GetDirectoryPath(It.IsAny<string>()))
            .Returns(GetPath());

        var mockUi = new Mock<IUserInterface>();

        var writer = GetInstance(config: config, fs: mockFs.Object, ui: mockUi.Object);
        var taggingFile = new TaggingFile
        {
            Path = GetPath("Movie (2020).mkv"),
            SubtitlePaths = [GetPath(inPath)]
        };

        var metadata = new MovieFileMetadata
        {
            Title = "Movie",
            Date = new DateTime(2020, 1, 1)
        };

        var result = await writer.WriteAsync(taggingFile, metadata);

        result.Should().BeTrue();
        mockFs.Verify(fs => fs.Move(inPath, outPath), Times.Once);
        mockUi.Verify(ui => ui.SetStatus("File skipped - already named correctly", MessageType.Information),
            Times.Never);
    }

    [Fact]
    public async Task Should_TagFile_WhenRenameIsSkipped()
    {
        var config = new AutoTagConfig
        {
            RenameFiles = false,
            TagFiles = true
        };
        var mockUi = new Mock<IUserInterface>();
        var writer = GetInstance(config: config, ui: mockUi.Object);
        var metadata = new MovieFileMetadata
        {
            Title = "Movie",
            Date = new DateTime(2020, 1, 1)
        };

        var result = await writer.WriteAsync(new TaggingFile { Path = GetPath("Movie (2020).mkv") }, metadata);

        result.Should().BeFalse();
        mockUi.Verify(
            ui => ui.SetStatus("Error: Failed to write tags to file", MessageType.Error, It.IsAny<Exception>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_RemoveSourceFolder_WhenSourceFolderIsEmptyAndAbsolutePatternUsed()
    {
        var config = new AutoTagConfig
        {
            MovieRenamePattern = GetPath("Movies", "{Title} ({Year})"),
            RemoveEmptyFolders = true,
            RenameFiles = true,
            TagFiles = false
        };

        var mockFs = new Mock<IFileSystem>();
        mockFs.Setup(fs => fs.Exists(GetPath("Movies", "Movie (2020).mkv")))
            .Returns(false);
        mockFs.Setup(fs => fs.DirectoryExists(GetPath("Downloads")))
            .Returns(true);
        mockFs.Setup(fs => fs.DirectoryIsEmpty(GetPath("Downloads")))
            .Returns(true);
        mockFs.Setup(fs => fs.GetDirectoryPath(It.Is((string s) => s.Contains("Downloads"))))
            .Returns(GetPath("Downloads"));
        mockFs.Setup(fs => fs.GetDirectoryPath(It.Is((string s) => s.Contains("Movies"))))
            .Returns(GetPath("Movies"));
        mockFs.Setup(fs => fs.PathContainsDirectory(It.IsAny<string>())).Returns(true);

        var downloads = GetPath("Downloads");

        var writer = GetInstance(config: config, fs: mockFs.Object);
        var taggingFile = new TaggingFile
        {
            Path = GetPath("Downloads", "raw.mkv")
        };
        var metadata = new MovieFileMetadata
        {
            Title = "Movie",
            Date = new DateTime(2020, 1, 1)
        };

        var result = await writer.WriteAsync(taggingFile, metadata);

        result.Should().BeTrue();
        mockFs.Verify(fs => fs.DeleteDirectory(downloads), Times.Once);
    }

    [Fact]
    public async Task Should_NotRemoveSourceFolder_WhenNotEmpty()
    {
        var config = new AutoTagConfig
        {
            MovieRenamePattern = GetPath("Movies", "{Title} ({Year})"),
            RemoveEmptyFolders = true,
            RenameFiles = true,
            TagFiles = false
        };

        var mockFs = new Mock<IFileSystem>();
        mockFs.Setup(fs => fs.Exists(GetPath("Movies", "Movie (2020).mkv")))
            .Returns(false);
        mockFs.Setup(fs => fs.DirectoryExists("Downloads"))
            .Returns(true);
        mockFs.Setup(fs => fs.DirectoryIsEmpty("Downloads"))
            .Returns(false);
        mockFs.Setup(fs => fs.GetDirectoryPath(It.IsAny<string>()))
            .Returns(GetPath());
        mockFs.Setup(fs => fs.PathContainsDirectory(It.IsAny<string>())).Returns(true);

        var writer = GetInstance(config: config, fs: mockFs.Object);
        var taggingFile = new TaggingFile
        {
            Path = GetPath("Downloads", "raw.mkv")
        };
        var metadata = new MovieFileMetadata
        {
            Title = "Movie",
            Date = new DateTime(2020, 1, 1)
        };

        var result = await writer.WriteAsync(taggingFile, metadata);

        result.Should().BeTrue();
        mockFs.Verify(fs => fs.DeleteDirectory(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Should_RenameMultipleSubtitlesWithNumberedSuffixes()
    {
        var config = new AutoTagConfig
        {
            RenameFiles = true,
            TagFiles = false
        };

        var mockFs = new Mock<IFileSystem>();
        mockFs.Setup(fs => fs.GetDirectoryPath(It.IsAny<string>()))
            .Returns(GetPath());

        var writer = GetInstance(config: config, fs: mockFs.Object);

        var sub1Path = GetPath("sub-one.ass");
        var sub2Path = GetPath("sub-two.ass");
        var taggingFile = new TaggingFile
        {
            Path = GetPath("raw.mkv"),
            SubtitlePaths = [sub1Path, sub2Path]
        };
        var metadata = new TVFileMetadata
        {
            SeriesName = "Series",
            Season = 1,
            Episode = 1,
            Title = "Pilot"
        };

        var result = await writer.WriteAsync(taggingFile, metadata);

        var sub1OutPath = GetPath("Series - 1x01 - Pilot.1.ass");
        var sub2OutPath = GetPath("Series - 1x01 - Pilot.2.ass");

        result.Should().BeTrue();
        mockFs.Verify(fs => fs.Move(
            sub1Path,
            sub1OutPath
        ), Times.Once);
        mockFs.Verify(fs => fs.Move(
            sub2Path,
            sub2OutPath
        ), Times.Once);
    }
}