using AutoTag.Core.Config;
using AutoTag.Core.Files;
using AutoTag.Core.TMDB;
using AutoTag.Core.TV;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;

namespace AutoTag.Core.Test.TV.TVProcessor;

public class ProcessAsync : TVProcessorTestBase
{
    [Fact]
    public async Task Should_ReturnFalse_When_FileNameCannotBeParsed()
    {
        var instance = GetInstance();

        var result = await instance.ProcessAsync(new TaggingFile
        {
            Path = "invalid file name"
        });

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Should_ReturnFalse_When_UnableToFindShow()
    {
        var mockTmdb = new Mock<ITMDBService>();
        mockTmdb.Setup(tmdb => tmdb.SearchTvShowAsync(It.IsAny<string>()))
            .ReturnsAsync(new SearchContainer<SearchTv> { Results = [] });

        var instance = GetInstance(tmdb: mockTmdb.Object);

        var result = await instance.ProcessAsync(new TaggingFile
        {
            Path = "/Show/Show S01E02.mp4"
        });

        result.Should().BeFalse();
        mockTmdb.Verify(tmdb => tmdb.SearchTvShowAsync(It.IsAny<string>()), Times.Once);
    }
    
    [Fact]
    public async Task Should_ReturnTrueAndShowWarning_When_FileSkipped()
    {
        var config = new AutoTagConfig { ManualMode = true };
        
        var mockTmdb = new Mock<ITMDBService>();
        mockTmdb.Setup(tmdb => tmdb.SearchTvShowAsync(It.IsAny<string>()))
            .ReturnsAsync(new SearchContainer<SearchTv> { Results = [new SearchTv { Name = "Show" }] });

        var mockUi = new Mock<IUserInterface>();
        mockUi.Setup(ui => ui.SelectOption(It.IsAny<string>(), It.IsAny<List<string>>()))
            .Returns((int?)null);

        var instance = GetInstance(tmdb: mockTmdb.Object, ui: mockUi.Object, config: config);

        var result = await instance.ProcessAsync(new TaggingFile
        {
            Path = "/Show/Show S01E02.mp4"
        });

        result.Should().BeTrue();
        mockUi.Verify(ui => ui.SetStatus("File skipped", MessageType.Warning));
    }
    
    [Fact]
    public async Task Should_ReturnFalse_When_FindEpisodeFails()
    {
        var mockCache = new Mock<ITVCache>();
        
        mockCache.Setup(c => c.ShowIsCached(It.IsAny<string>()))
            .Returns(true);

        var show = new ShowResults(new SearchTv { Name = "Show" });
        show.AddEpisodeGroup(
            new TvGroupCollection
            {
                Groups =
                [
                    new TvGroup
                    {
                        Name = "Season 2",
                        Episodes =
                        [
                            new TvGroupEpisode
                            {
                                Order = 0,
                                SeasonNumber = 2,
                                EpisodeNumber = 1
                            }
                        ]
                    }
                ]
            },
            out _
        );
        
        mockCache.Setup(c => c.GetShow(It.IsAny<string>()))
            .Returns([show]);
        
        TvSeason? season = null;
        mockCache.Setup(c => c.TryGetSeason(It.IsAny<int>(), It.IsAny<int>(), out season))
            .Returns(false);

        var mockUi = new Mock<IUserInterface>();

        var instance = GetInstance(cache: mockCache.Object, ui: mockUi.Object);

        var result = await instance.ProcessAsync(new TaggingFile
        {
            Path = "/Show/Show S01E02.mp4"
        });

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Should_ReturnFalseAndShowError_When_ReachedEndOfSearchResultsWithoutFindingEpisode()
    {
        var mockCache = new Mock<ITVCache>();
        
        mockCache.Setup(c => c.ShowIsCached(It.IsAny<string>()))
            .Returns(true);
        
        mockCache.Setup(c => c.GetShow(It.IsAny<string>()))
            .Returns([ new ShowResults(new SearchTv { Name = "Show" }) ]);
        
        TvSeason? season = null;
        mockCache.Setup(c => c.TryGetSeason(It.IsAny<int>(), It.IsAny<int>(), out season))
            .Returns(false);

        var mockUi = new Mock<IUserInterface>();

        var instance = GetInstance(cache: mockCache.Object, ui: mockUi.Object);

        var result = await instance.ProcessAsync(new TaggingFile
        {
            Path = "/Show/Show S01E02.mp4"
        });

        result.Should().BeFalse();
        mockUi.Verify(ui => ui.SetStatus("Error: Cannot find Show S01E02 on TheMovieDB", MessageType.Error));
    }

    [Fact]
    public async Task Should_FindCoverArtFromSeason_When_NoCoverFromEpisode()
    {
        var mockCache = new Mock<ITVCache>();
        
        mockCache.Setup(c => c.ShowIsCached(It.IsAny<string>()))
            .Returns(true);
        
        mockCache.Setup(c => c.GetShow(It.IsAny<string>()))
            .Returns([ new ShowResults(new SearchTv { Name = "Show" }) ]);
        
        var season = new TvSeason
        {
            Episodes = [new TvSeasonEpisode { EpisodeNumber = 2 }]
        };
        mockCache.Setup(c => c.TryGetSeason(It.IsAny<int>(), It.IsAny<int>(), out season))
            .Returns(true);

        var poster = "poster-url";
        mockCache.Setup(c => c.TryGetSeasonPoster(It.IsAny<int>(), It.IsAny<int>(), out poster))
            .Returns(true);

        var mockWriter = new Mock<IFileWriter>();
        mockWriter.Setup(w => w.WriteAsync(It.IsAny<TaggingFile>(), It.IsAny<FileMetadata>()))
            .ReturnsAsync(true);

        var instance = GetInstance(cache: mockCache.Object, writer: mockWriter.Object);

        var result = await instance.ProcessAsync(new TaggingFile
        {
            Path = "/Show/Show S01E02.mp4",
            Taggable = true
        });

        result.Should().BeTrue();
        mockCache.Verify(c => c.TryGetSeasonPoster(It.IsAny<int>(), It.IsAny<int>(), out poster));
    }

    [Fact]
    public async Task Should_WriteFileAndReturnTrue_When_Succeeds()
    {
        var config = new AutoTagConfig { AddCoverArt = false };
        
        var mockCache = new Mock<ITVCache>();
        
        mockCache.Setup(c => c.ShowIsCached(It.IsAny<string>()))
            .Returns(true);
        
        mockCache.Setup(c => c.GetShow(It.IsAny<string>()))
            .Returns([ new ShowResults(new SearchTv { Name = "Show" }) ]);
        
        var season = new TvSeason
        {
            Episodes = [new TvSeasonEpisode { EpisodeNumber = 2 }]
        };
        mockCache.Setup(c => c.TryGetSeason(It.IsAny<int>(), It.IsAny<int>(), out season))
            .Returns(true);

        var mockWriter = new Mock<IFileWriter>();
        mockWriter.Setup(w => w.WriteAsync(It.IsAny<TaggingFile>(), It.IsAny<FileMetadata>()))
            .ReturnsAsync(true);

        var instance = GetInstance(cache: mockCache.Object, writer: mockWriter.Object, config: config);

        var result = await instance.ProcessAsync(new TaggingFile
        {
            Path = "/Show/Show S01E02.mp4"
        });

        result.Should().BeTrue();
        mockWriter.Verify(w => w.WriteAsync(It.IsAny<TaggingFile>(), It.IsAny<FileMetadata>()));
    }
}