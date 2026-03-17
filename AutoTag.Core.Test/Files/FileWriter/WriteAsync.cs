using AutoTag.Core.Config;
using AutoTag.Core.Files;
using AutoTag.Core.Movie;

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
}
