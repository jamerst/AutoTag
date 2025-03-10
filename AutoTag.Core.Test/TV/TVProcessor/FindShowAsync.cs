using AutoTag.Core.Config;
using AutoTag.Core.TMDB;
using AutoTag.Core.TV;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;

namespace AutoTag.Core.Test.TV.TVProcessor;

public class FindShowAsync : TVProcessorTestBase
{
    [Fact]
    public async Task Should_NotQueryAPI_When_ShowAlreadyInCache()
    {
        var mockCache = new Mock<ITVCache>();
        mockCache.Setup(c => c.ShowIsCached(It.IsAny<string>())).Returns(true);

        var mockTmdb = new Mock<ITMDBService>();

        var tv = GetInstance(tmdb: mockTmdb.Object, cache: mockCache.Object);

        var result = await tv.FindShowAsync("a");

        result.Should().Be(FindResult.Success);
        mockTmdb.Verify(t => t.SearchTvShowAsync(It.IsAny<string>()), Times.Never);
    }
    
    [Fact]
    public async Task Should_ReportError_When_NoTMDBSearchResults()
    {
        var mockTmdb = new Mock<ITMDBService>();
        mockTmdb.Setup(tmdb => tmdb.SearchTvShowAsync(It.IsAny<string>()))
            .ReturnsAsync(new SearchContainer<SearchTv>
            {
                Results = []
            });

        var mockUi = new Mock<IUserInterface>();

        var tv = GetInstance(tmdb: mockTmdb.Object, ui: mockUi.Object);

        var result = await tv.FindShowAsync("series name");

        result.Should().Be(FindResult.Fail);
        mockUi.Verify(ui => ui.SetStatus("Error: Cannot find series series name on TheMovieDB", MessageType.Error),
            Times.Once
        );
    }
    
    [Fact]
    public async Task Should_OnlyCacheSelectedResult_When_ManualModeEnabled()
    {
        var config = new AutoTagConfig
        {
            ManualMode = true
        };

        var selectedResult = new SearchTv
        {
            Id = 2,
            Name = "2"
        };
        List<SearchTv> results =
        [
            new()
            {
                Id = 1,
                Name = "1"
            },
            selectedResult,
            new()
            {
                Id = 3,
                Name = "3"
            }
        ];

        var mockTmdb = new Mock<ITMDBService>();
        mockTmdb.Setup(tmdb => tmdb.SearchTvShowAsync(It.IsAny<string>()))
            .ReturnsAsync(new SearchContainer<SearchTv>
            {
                Results = results
            });

        var mockCache = new Mock<ITVCache>();

        var mockUi = new Mock<IUserInterface>();
        mockUi.Setup(ui => ui.SelectOption(It.IsAny<string>(), It.IsAny<List<string>>()))
            .Returns(results.IndexOf(selectedResult));

        var tv = GetInstance(tmdb: mockTmdb.Object, cache: mockCache.Object, ui: mockUi.Object, config: config);

        var result = await tv.FindShowAsync("a");

        result.Should().Be(FindResult.Success);
        mockUi.Verify(ui => ui.SelectOption("Please choose an option:", It.IsAny<List<string>>()), Times.Once);
        mockCache.Verify(c => c.AddShow(
                It.IsAny<string>(),
                It.Is<List<ShowResults>>(s => s.Count == 1 && s[0].TvSearchResult.Id == selectedResult.Id)),
            Times.Once
        );
    }
    
    [Fact]
    public async Task Should_SkipFile_When_SelectOptionReturnsNull()
    {
        var config = new AutoTagConfig
        {
            ManualMode = true
        };
        
        var mockTmdb = new Mock<ITMDBService>();
        mockTmdb.Setup(tmdb => tmdb.SearchTvShowAsync(It.IsAny<string>()))
            .ReturnsAsync(new SearchContainer<SearchTv>
            {
                Results = [new SearchTv { Name = "" }]
            });
        
        var mockUi = new Mock<IUserInterface>();
        mockUi.Setup(ui => ui.SelectOption(It.IsAny<string>(), It.IsAny<List<string>>()))
            .Returns((int?)null);

        var tv = GetInstance(tmdb: mockTmdb.Object, ui: mockUi.Object, config: config);

        var result = await tv.FindShowAsync("");
        
        result.Should().Be(FindResult.Skip);
    }

    [Fact]
    public async Task Should_ReturnResultFromFindEpisodeGroupAsync_When_NotNull()
    {
        var config = new AutoTagConfig
        {
            EpisodeGroup = true
        };
        
        var mockTmdb = new Mock<ITMDBService>();
        mockTmdb.Setup(tmdb => tmdb.SearchTvShowAsync(It.IsAny<string>()))
            .ReturnsAsync(new SearchContainer<SearchTv>
            {
                Results = [new SearchTv { Name = "" }]
            });
        
        mockTmdb.Setup(tmdb => tmdb.GetTvShowWithEpisodeGroupsAsync(It.IsAny<int>()))
            .ReturnsAsync(new TvShow
            {
                EpisodeGroups = new ResultContainer<TvGroupCollection>
                {
                    Results = [new TvGroupCollection()]
                }
            });

        var tv = GetInstance(tmdb: mockTmdb.Object, config: config);

        var result = await tv.FindShowAsync("");
        result.Should().Be(FindResult.Skip);
    }

    [Fact]
    public async Task Should_OnlyCacheSelectedResult_When_EpisodeGroupSelected()
    {
        var config = new AutoTagConfig
        {
            EpisodeGroup = true
        };
        
        var mockTmdb = new Mock<ITMDBService>();

        var expectedShow = new SearchTv { Name = "Show 1" };
        mockTmdb.Setup(tmdb => tmdb.SearchTvShowAsync(It.IsAny<string>()))
            .ReturnsAsync(new SearchContainer<SearchTv>
            {
                Results = [expectedShow, new SearchTv { Name = "Show 2" }]
            });
        
        mockTmdb.Setup(tmdb => tmdb.GetTvShowWithEpisodeGroupsAsync(It.IsAny<int>()))
            .ReturnsAsync(new TvShow
            {
                EpisodeGroups = new ResultContainer<TvGroupCollection>
                {
                    Results = [new TvGroupCollection()]
                }
            });

        mockTmdb.Setup(tmdb => tmdb.GetTvEpisodeGroupsAsync(It.IsAny<string>()))
            .ReturnsAsync(new TvGroupCollection
            {
                Groups = [new TvGroup
                {
                    Name = "Season 1",
                    Episodes = []
                }]
            });

        var mockUi = new Mock<IUserInterface>();
        mockUi.Setup(ui => ui.SelectOption(It.IsAny<string>(), It.IsAny<List<string>>()))
            .Returns(0);

        var mockCache = new Mock<ITVCache>();

        var tv = GetInstance(tmdb: mockTmdb.Object, ui: mockUi.Object, cache: mockCache.Object, config: config);

        await tv.FindShowAsync("");
        
        mockCache.Verify(c => c.AddShow(
            It.IsAny<string>(),
            It.Is<List<ShowResults>>(results => results.Count == 1
                && results.Single().TvSearchResult == expectedShow)
        ));
    }
}