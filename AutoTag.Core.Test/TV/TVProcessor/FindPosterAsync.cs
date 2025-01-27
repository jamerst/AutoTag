using AutoTag.Core.TMDB;
using AutoTag.Core.TV;
using TMDbLib.Objects.General;

namespace AutoTag.Core.Test.TV.TVProcessor;

public class FindPosterAsync : TVProcessorTestBase
{
    [Fact]
    public async Task Should_UsePosterUrlFromCache_WhenAvailable()
    {
        var mockCache = new Mock<ITVCache>();
        var url = "https://some.url.com/";
        mockCache.Setup(c => c.TryGetSeasonPoster(It.IsAny<int>(), It.IsAny<int>(), out url))
            .Returns(true);

        var metadata = new TVFileMetadata();
        
        var instance = GetInstance(cache: mockCache.Object);

        await instance.FindPosterAsync(metadata);

        metadata.CoverURL.Should().Be(url);
    }

    [Fact]
    public async Task Should_UseHighestRatedPoster_When_NotCached()
    {
        var mockTmdb = new Mock<ITMDBService>();
        mockTmdb.Setup(tmdb => tmdb.GetTvShowImagesAsync(It.IsAny<int>()))
            .ReturnsAsync(new ImagesWithId
            {
                Posters =
                [
                    new ImageData { FilePath = "file1", VoteAverage = 1 },
                    new ImageData { FilePath = "file2", VoteAverage = 10 },
                    new ImageData { FilePath = "file3", VoteAverage = 2.5 }
                ]
            });

        var metadata = new TVFileMetadata();

        var instance = GetInstance(tmdb: mockTmdb.Object);

        await instance.FindPosterAsync(metadata);

        metadata.CoverURL.Should().Be("https://image.tmdb.org/t/p/original/file2");
    }
    
    [Fact]
    public async Task Should_ReportErrorAndMarkAsIncomplete_When_NoPostersFound()
    {
        var mockTmdb = new Mock<ITMDBService>();
        mockTmdb.Setup(tmdb => tmdb.GetTvShowImagesAsync(It.IsAny<int>()))
            .ReturnsAsync(new ImagesWithId
            {
                Posters = []
            });

        var mockUi = new Mock<IUserInterface>();

        var metadata = new TVFileMetadata();

        var instance = GetInstance(tmdb: mockTmdb.Object, ui: mockUi.Object);

        await instance.FindPosterAsync(metadata);

        metadata.Complete.Should().BeFalse();
        mockUi.Verify(ui => ui.SetStatus("Error: Failed to find episode cover", MessageType.Error));
    }
}