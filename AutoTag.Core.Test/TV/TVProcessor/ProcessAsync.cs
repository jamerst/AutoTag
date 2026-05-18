using AutoTag.Core.Config;
using AutoTag.Core.Files;
using AutoTag.Core.Files.Parsing;
using AutoTag.Core.TMDB;
using AutoTag.Core.TV;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;

namespace AutoTag.Core.Test.TV.TVProcessor;

public class ProcessAsync : TVProcessorTestBase
{
    [Fact]
    public async Task Should_ReturnParseFailure_When_FileNameCannotBeParsed()
    {
        var instance = GetInstance();

        var result = await instance.ProcessAsync(new TaggingFile
        {
            Path = "invalid file name"
        });

        result.Should().Be(ProcessResult.ParseFailure);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_UnableToFindShow()
    {
        var mockTmdb = new Mock<ITMDBService>();
        mockTmdb.Setup(tmdb => tmdb.SearchTvShowAsync(It.IsAny<string>()))
            .ReturnsAsync(new SearchContainer<SearchTv> { Results = [] });

        var instance = GetInstance(mockTmdb.Object);

        var result = await instance.ProcessAsync(new TaggingFile
        {
            Path = "/Show/Show S01E02.mp4",
            TVDetails = new ParsedTVFileName("Show", null, 1, 2, null, null)
        });

        result.Should().Be(ProcessResult.NotFound);
        mockTmdb.Verify(tmdb => tmdb.SearchTvShowAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnSkippedAndShowWarning_When_FileSkipped()
    {
        var config = new AutoTagConfig { ManualMode = true };

        var mockTmdb = new Mock<ITMDBService>();
        mockTmdb.Setup(tmdb => tmdb.SearchTvShowAsync(It.IsAny<string>()))
            .ReturnsAsync(new SearchContainer<SearchTv> { Results = [new SearchTv { Name = "Show" }] });

        var mockUi = new Mock<IUserInterface>();
        mockUi.Setup(ui => ui.SelectOption(It.IsAny<string>(), It.IsAny<List<string>>()))
            .Returns((int?)null);

        var instance = GetInstance(mockTmdb.Object, ui: mockUi.Object, config: config);

        var result = await instance.ProcessAsync(new TaggingFile
        {
            Path = "/Show/Show S01E02.mp4",
            TVDetails = new ParsedTVFileName("Show", null, 1, 2, null, null)
        });

        result.Should().Be(ProcessResult.Skipped);
        mockUi.Verify(ui => ui.SetStatus("File skipped", MessageType.Warning));
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_FindEpisodeFails()
    {
        var mockCache = new Mock<ITVCache>();

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
        List<ShowResults>? showResults = [show];
        mockCache.Setup(c => c.TryGetShow(It.IsAny<string>(), It.IsAny<int?>(), out showResults))
            .Returns(true);

        TvSeason? season = null;
        mockCache.Setup(c => c.TryGetSeason(It.IsAny<int>(), It.IsAny<int>(), out season))
            .Returns(false);

        var mockUi = new Mock<IUserInterface>();

        var instance = GetInstance(cache: mockCache.Object, ui: mockUi.Object);

        var result = await instance.ProcessAsync(new TaggingFile
        {
            Path = "/Show/Show S01E02.mp4",
            TVDetails = new ParsedTVFileName("Show", null, 1, 2, null, null)
        });

        result.Should().Be(ProcessResult.NotFound);
    }

    [Fact]
    public async Task Should_ReturnNotFoundAndShowError_When_ReachedEndOfSearchResultsWithoutFindingEpisode()
    {
        var mockCache = new Mock<ITVCache>();

        List<ShowResults>? showResults = [new ShowResults(new SearchTv { Name = "Show" })];
        mockCache.Setup(c => c.TryGetShow(It.IsAny<string>(), It.IsAny<int?>(), out showResults))
            .Returns(true);

        TvSeason? season = null;
        mockCache.Setup(c => c.TryGetSeason(It.IsAny<int>(), It.IsAny<int>(), out season))
            .Returns(false);

        var mockUi = new Mock<IUserInterface>();

        var instance = GetInstance(cache: mockCache.Object, ui: mockUi.Object);

        var result = await instance.ProcessAsync(new TaggingFile
        {
            Path = "/Show/Show S01E02.mp4",
            TVDetails = new ParsedTVFileName("Show", null, 1, 2, null, null)
        });

        result.Should().Be(ProcessResult.NotFound);
        mockUi.Verify(ui => ui.SetStatus("Error: Cannot find Show S01E02 on TheMovieDB", MessageType.Error));
    }

    [Fact]
    public async Task Should_FindCoverArtFromSeason_When_NoCoverFromEpisode()
    {
        var mockCache = new Mock<ITVCache>();

        List<ShowResults>? showResults = [new ShowResults(new SearchTv { Name = "Show" })];
        mockCache.Setup(c => c.TryGetShow(It.IsAny<string>(), It.IsAny<int?>(), out showResults))
            .Returns(true);

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
            TVDetails = new ParsedTVFileName("Show", null, 1, 2, null, null),
            Taggable = true
        });

        result.Should().Be(ProcessResult.Success);
        mockCache.Verify(c => c.TryGetSeasonPoster(It.IsAny<int>(), It.IsAny<int>(), out poster));
    }

    [Fact]
    public async Task Should_WriteFileAndReturnSuccess_When_Succeeds()
    {
        var config = new AutoTagConfig { AddCoverArt = false };

        var mockCache = new Mock<ITVCache>();

        List<ShowResults>? showResults = [new ShowResults(new SearchTv { Name = "Show" })];
        mockCache.Setup(c => c.TryGetShow(It.IsAny<string>(), It.IsAny<int?>(), out showResults))
            .Returns(true);

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
            Path = "/Show/Show S01E02.mp4",
            TVDetails = new ParsedTVFileName("Show", null, 1, 2, null, null)
        });

        result.Should().Be(ProcessResult.Success);
        mockWriter.Verify(w => w.WriteAsync(It.IsAny<TaggingFile>(), It.IsAny<FileMetadata>()));
    }
}