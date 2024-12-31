using AutoTag.Core.TMDB;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;

namespace AutoTag.Core.Test.TV.TVProcessor;

public class FindEpisodeGroupAsync : TVProcessorTestBase
{
    private static void SetupGetTvShowWithEpisodeGroupsAsync(Mock<ITMDBService> mockTmdb)
        => mockTmdb.Setup(tmdb => tmdb.GetTvShowWithEpisodeGroupsAsync(It.IsAny<int>()))
            .ReturnsAsync(new TvShow
            {
                EpisodeGroups = new ResultContainer<TvGroupCollection>
                {
                    Results =
                    [
                        new TvGroupCollection
                        {
                            Name = "Grouping 1"
                        },
                        new TvGroupCollection
                        {
                            Name = "Grouping 2"
                        }
                    ]
                }
            });
    
    [Fact]
    public async Task Should_AddSkipToNextSearchResultOption_When_MultipleSearchResults()
    {
        var mockTmdb = new Mock<ITMDBService>();
        SetupGetTvShowWithEpisodeGroupsAsync(mockTmdb);

        List<List<string>> passedOptions = [];
        var mockUi = new Mock<IUserInterface>();
        mockUi.Setup(ui => ui.SelectOption(It.IsAny<string>(), It.IsAny<List<string>>()))
            .Returns((string msg, List<string> options) => msg.Contains("Show 1") ? options.Count - 1 : 0) // skip to next search result for first result, first option otherwise
            .Callback((string _, List<string> options) => passedOptions.Add(options));

        var instance = GetInstance(tmdb: mockTmdb.Object, ui: mockUi.Object);

        await instance.FindEpisodeGroupAsync([new SearchTv { Name = "Show 1" }, new SearchTv { Name = "Show 2" }]);

        passedOptions.Count.Should().Be(2);
        passedOptions[0].Should().Contain("(Skip to next search result)"); // option should be present for first search result
        passedOptions[^1].Should().NotContain("(Skip to next search result)"); // option should not be present for last search result (since there is no next search result to skip to)
    }

    [Fact]
    public async Task Should_ReturnSelectedShowResultsAndSetMappingTable_When_GroupingSelected()
    {
        var mockTmdb = new Mock<ITMDBService>();
        SetupGetTvShowWithEpisodeGroupsAsync(mockTmdb);
        
        var mockUi = new Mock<IUserInterface>();
        mockUi.Setup(ui => ui.SelectOption(It.IsAny<string>(), It.IsAny<List<string>>()))
            .Returns((string msg, List<string> options) => msg.Contains("Show 1") ? options.Count - 1 : 0);

        var instance = GetInstance(tmdb: mockTmdb.Object, ui: mockUi.Object);

        var show2 = new SearchTv { Name = "Show 2" };

        var (result, newShow) = await instance.FindEpisodeGroupAsync([new SearchTv { Name = "Show 1" }, show2]);

        result.Should().Be(FindResult.Success);
        newShow.Should().Be(show2);
        newShow!.HasEpisodeGroupMapping.Should().BeTrue();
    }

    [Fact]
    public async Task Should_ReportError_When_GetTvShowWithEpisodeGroupsAsyncReturnsNull()
    {
        var mockTmdb = new Mock<ITMDBService>();
        mockTmdb.Setup(tmdb => tmdb.GetTvShowWithEpisodeGroupsAsync(It.IsAny<int>()))
            .ReturnsAsync((TvShow)null!);

        var mockUi = new Mock<IUserInterface>();
        
        var instance = GetInstance(tmdb: mockTmdb.Object, ui: mockUi.Object);

        var (result, _) = await instance.FindEpisodeGroupAsync([new SearchTv()]);
        
        mockUi.Verify(ui => ui.SetStatus(It.IsAny<string>(), MessageType.Error));
        result.Should().Be(FindResult.Fail);
    }
    
    [Theory]
    [InlineData("Season 1 Part 1", "Season 1 Part 2")] // duplicate season 1
    [InlineData("Part One", "Part Two")] // no numbers in name
    public async Task Should_ReportError_When_EpisodeGroupNotValid(string groupName1, string groupName2)
    {
        var mockTmdb = new Mock<ITMDBService>();
        SetupGetTvShowWithEpisodeGroupsAsync(mockTmdb);
        mockTmdb.Setup(tmdb => tmdb.GetTvEpisodeGroupsAsync(It.IsAny<string>()))
            .ReturnsAsync(new TvGroupCollection
            {
                Groups = [
                    new TvGroup
                    {
                        Name = groupName1,
                        Episodes = [
                            new TvGroupEpisode
                            {
                                Order = 0,
                                SeasonNumber = 1,
                                EpisodeNumber = 1
                            }
                        ]
                    },
                    new TvGroup
                    {
                        Name = groupName2,
                        Episodes = [
                            new TvGroupEpisode
                            {
                                Order = 0,
                                SeasonNumber = 1,
                                EpisodeNumber = 1
                            }
                        ]
                    }
                ]
            });

        var mockUi = new Mock<IUserInterface>();
        mockUi.Setup(ui => ui.SelectOption(It.IsAny<string>(), It.IsAny<List<string>>()))
            .Returns(0);
        
        var instance = GetInstance(tmdb: mockTmdb.Object, ui: mockUi.Object);

        var (result, _) = await instance.FindEpisodeGroupAsync([new SearchTv()]);
        
        mockUi.Verify(ui => ui.SetStatus(It.IsAny<string>(), MessageType.Error));
        result.Should().Be(FindResult.Fail);
    }

    [Fact]
    public async Task Should_ReturnFindResultSkip_When_NoOptionSelected()
    {
        var mockTmdb = new Mock<ITMDBService>();
        SetupGetTvShowWithEpisodeGroupsAsync(mockTmdb);

        var mockUi = new Mock<IUserInterface>();
        mockUi.Setup(ui => ui.SelectOption(It.IsAny<string>(), It.IsAny<List<string>>()))
            .Returns((int?)null);
        
        var instance = GetInstance(tmdb: mockTmdb.Object, ui: mockUi.Object);

        var (result, _) = await instance.FindEpisodeGroupAsync([new SearchTv { Name = "Show 1" }]);

        result.Should().Be(FindResult.Skip);
    }
}