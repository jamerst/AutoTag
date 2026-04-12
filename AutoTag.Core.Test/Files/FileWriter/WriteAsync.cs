using AutoTag.Core.Config;
using AutoTag.Core.Files;
using AutoTag.Core.Movie;
using AutoTag.Core.TV;

namespace AutoTag.Core.Test.Files.FileWriter;

public class WriteAsync
{
    [Fact]
    public async Task Should_Skip_When_Video_And_Subtitle_Are_Already_Correctly_Named()
    {
        var config = new AutoTagConfig
        {
            RenameFiles = true,
            TagFiles = false
        };

        var mockFs = new Mock<IFileSystem>();
        var mockUi = new Mock<IUserInterface>();
        var mockCoverArtFetcher = new Mock<ICoverArtFetcher>();

        var writer = new Core.Files.FileWriter(mockCoverArtFetcher.Object, config, mockFs.Object, mockUi.Object);
        var taggingFile = new TaggingFile
        {
            Path = @"C:\Media\Movie (2020).mkv",
            SubtitlePath = @"C:\Media\Movie (2020).srt"
        };

        var metadata = new MovieFileMetadata
        {
            Title = "Movie",
            Date = new DateTime(2020, 1, 1)
        };

        var result = await writer.WriteAsync(taggingFile, metadata);

        result.Should().BeTrue();
        mockFs.Verify(fs => fs.Move(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        mockUi.Verify(ui => ui.SetStatus("File skipped - already named correctly", MessageType.Information), Times.Once);
    }

    [Fact]
    public async Task Should_Not_Skip_When_Subtitle_Name_Is_Still_Wrong()
    {
        var config = new AutoTagConfig
        {
            RenameFiles = true,
            TagFiles = false
        };

        var mockFs = new Mock<IFileSystem>();
        mockFs.Setup(fs => fs.Exists(@"C:\Media\Movie (2020).srt"))
            .Returns(false);

        var mockUi = new Mock<IUserInterface>();
        var mockCoverArtFetcher = new Mock<ICoverArtFetcher>();

        var writer = new Core.Files.FileWriter(mockCoverArtFetcher.Object, config, mockFs.Object, mockUi.Object);
        var taggingFile = new TaggingFile
        {
            Path = @"C:\Media\Movie (2020).mkv",
            SubtitlePath = @"C:\Media\subtitle.srt"
        };

        var metadata = new MovieFileMetadata
        {
            Title = "Movie",
            Date = new DateTime(2020, 1, 1)
        };

        var result = await writer.WriteAsync(taggingFile, metadata);

        result.Should().BeTrue();
        mockFs.Verify(fs => fs.Move(@"C:\Media\subtitle.srt", @"C:\Media\Movie (2020).srt"), Times.Once);
        mockUi.Verify(ui => ui.SetStatus("File skipped - already named correctly", MessageType.Information), Times.Never);
    }

    [Fact]
    public async Task Should_Move_Movie_Into_Named_Folder_When_OrganizeFolders_Enabled()
    {
        var config = new AutoTagConfig
        {
            OrganizeFolders = true,
            RenameFiles = true,
            TagFiles = false
        };

        var mockFs = new Mock<IFileSystem>();
        mockFs.Setup(fs => fs.Exists(@"C:\Media\Movie (2020)\Movie (2020).mkv"))
            .Returns(false);

        var mockUi = new Mock<IUserInterface>();
        var mockCoverArtFetcher = new Mock<ICoverArtFetcher>();

        var writer = new Core.Files.FileWriter(mockCoverArtFetcher.Object, config, mockFs.Object, mockUi.Object);
        var taggingFile = new TaggingFile
        {
            Path = @"C:\Media\Downloads\raw.mkv",
            RootPath = @"C:\Media"
        };

        var metadata = new MovieFileMetadata
        {
            Title = "Movie",
            Date = new DateTime(2020, 1, 1)
        };

        var result = await writer.WriteAsync(taggingFile, metadata);

        result.Should().BeTrue();
        mockFs.Verify(fs => fs.CreateDirectory(It.Is<DirectoryInfo>(d => d.FullName == @"C:\Media\Movie (2020)")), Times.Once);
        mockFs.Verify(fs => fs.Move(@"C:\Media\Downloads\raw.mkv", @"C:\Media\Movie (2020)\Movie (2020).mkv"), Times.Once);
    }

    [Fact]
    public async Task Should_Move_TV_Special_Into_Specials_Folder_When_OrganizeFolders_Enabled()
    {
        var config = new AutoTagConfig
        {
            OrganizeFolders = true,
            RenameFiles = true,
            TagFiles = false
        };

        var mockFs = new Mock<IFileSystem>();
        mockFs.Setup(fs => fs.Exists(@"C:\Media\Series\Specials\Series - 0x01 - Pilot.mkv"))
            .Returns(false);

        var mockUi = new Mock<IUserInterface>();
        var mockCoverArtFetcher = new Mock<ICoverArtFetcher>();

        var writer = new Core.Files.FileWriter(mockCoverArtFetcher.Object, config, mockFs.Object, mockUi.Object);
        var taggingFile = new TaggingFile
        {
            Path = @"C:\Media\Downloads\raw.mkv",
            RootPath = @"C:\Media"
        };

        var metadata = new TVFileMetadata
        {
            SeriesName = "Series",
            Season = 0,
            Episode = 1,
            Title = "Pilot"
        };

        var result = await writer.WriteAsync(taggingFile, metadata);

        result.Should().BeTrue();
        mockFs.Verify(fs => fs.CreateDirectory(It.Is<DirectoryInfo>(d => d.FullName == @"C:\Media\Series\Specials")), Times.Once);
        mockFs.Verify(fs => fs.Move(@"C:\Media\Downloads\raw.mkv", @"C:\Media\Series\Specials\Series - 0x01 - Pilot.mkv"), Times.Once);
    }

    [Fact]
    public async Task Should_Remove_Source_Folder_When_Organized_And_Source_Folder_Is_Empty()
    {
        var config = new AutoTagConfig
        {
            OrganizeFolders = true,
            RemoveEmptyFolders = true,
            RenameFiles = true,
            TagFiles = false
        };

        var mockFs = new Mock<IFileSystem>();
        mockFs.Setup(fs => fs.Exists(@"C:\Media\Movie (2020)\Movie (2020).mkv"))
            .Returns(false);
        mockFs.Setup(fs => fs.DirectoryExists(@"C:\Media\Downloads"))
            .Returns(true);
        mockFs.Setup(fs => fs.DirectoryIsEmpty(@"C:\Media\Downloads"))
            .Returns(true);

        var writer = new Core.Files.FileWriter(
            new Mock<ICoverArtFetcher>().Object,
            config,
            mockFs.Object,
            new Mock<IUserInterface>().Object
        );
        var taggingFile = new TaggingFile
        {
            Path = @"C:\Media\Downloads\raw.mkv",
            RootPath = @"C:\Media"
        };
        var metadata = new MovieFileMetadata
        {
            Title = "Movie",
            Date = new DateTime(2020, 1, 1)
        };

        var result = await writer.WriteAsync(taggingFile, metadata);

        result.Should().BeTrue();
        mockFs.Verify(fs => fs.DeleteDirectory(@"C:\Media\Downloads"), Times.Once);
    }

    [Fact]
    public async Task Should_Not_Remove_Source_Folder_When_It_Is_Not_Empty()
    {
        var config = new AutoTagConfig
        {
            OrganizeFolders = true,
            RemoveEmptyFolders = true,
            RenameFiles = true,
            TagFiles = false
        };

        var mockFs = new Mock<IFileSystem>();
        mockFs.Setup(fs => fs.Exists(@"C:\Media\Movie (2020)\Movie (2020).mkv"))
            .Returns(false);
        mockFs.Setup(fs => fs.DirectoryExists(@"C:\Media\Downloads"))
            .Returns(true);
        mockFs.Setup(fs => fs.DirectoryIsEmpty(@"C:\Media\Downloads"))
            .Returns(false);

        var writer = new Core.Files.FileWriter(
            new Mock<ICoverArtFetcher>().Object,
            config,
            mockFs.Object,
            new Mock<IUserInterface>().Object
        );
        var taggingFile = new TaggingFile
        {
            Path = @"C:\Media\Downloads\raw.mkv",
            RootPath = @"C:\Media"
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
    public async Task Should_Rename_Multiple_Subtitles_With_Numbered_Suffixes()
    {
        var config = new AutoTagConfig
        {
            OrganizeFolders = true,
            RenameFiles = true,
            TagFiles = false
        };

        var mockFs = new Mock<IFileSystem>();
        var writer = new Core.Files.FileWriter(
            new Mock<ICoverArtFetcher>().Object,
            config,
            mockFs.Object,
            new Mock<IUserInterface>().Object
        );
        var taggingFile = new TaggingFile
        {
            Path = @"C:\Media\Downloads\raw.mkv",
            RootPath = @"C:\Media",
            SubtitlePaths =
            [
                @"C:\Media\Downloads\sub-one.ass",
                @"C:\Media\Downloads\sub-two.ass"
            ]
        };
        var metadata = new TVFileMetadata
        {
            SeriesName = "Series",
            Season = 1,
            Episode = 1,
            Title = "Pilot"
        };

        var result = await writer.WriteAsync(taggingFile, metadata);

        result.Should().BeTrue();
        mockFs.Verify(fs => fs.Move(
            @"C:\Media\Downloads\sub-one.ass",
            @"C:\Media\Series\Season 01\Series - 1x01 - Pilot.1.ass"
        ), Times.Once);
        mockFs.Verify(fs => fs.Move(
            @"C:\Media\Downloads\sub-two.ass",
            @"C:\Media\Series\Season 01\Series - 1x01 - Pilot.2.ass"
        ), Times.Once);
    }
}
