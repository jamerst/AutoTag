using AutoTag.Core.Config;
using AutoTag.Core.TMDB;
using AutoTag.Core.TV;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;

namespace AutoTag.Core.Test.TV.TVProcessor;

public class FindEpisodeAsync : TVProcessorTestBase
{
    [Fact]
    public async Task Should_SetEpisodeDetails_OnResult()
    {
        var mockCache = new Mock<ITVCache>();

        var season = new TvSeason
        {
            Episodes =
            [
                new TvSeasonEpisode
                {
                    Name = "S01E01",
                    EpisodeNumber = 1,
                    Overview = "Episode one"
                },
                new TvSeasonEpisode
                {
                    Name = "S01E02",
                    EpisodeNumber = 2,
                    Overview = "Episode two"
                }
            ],
            PosterPath = "poster"
        };
        mockCache.Setup(c => c.TryGetSeason(It.IsAny<int>(), It.IsAny<int>(), out season))
            .Returns(true);

        var mockTmdb = new Mock<ITMDBService>();
        mockTmdb.Setup(tmdb => tmdb.GetTvGenreNamesAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(["Genre 1"]);

        var show = new ShowResults(new SearchTv
        {
            Id = 1,
            Name = "Show Name",
            GenreIds = [1]
        });
        
        var metadata = new TVFileMetadata
        {
            Season = 1,
            Episode = 2
        };

        var instance = GetInstance(tmdb: mockTmdb.Object, cache: mockCache.Object);

        var (result, _) = await instance.FindEpisodeAsync(metadata, show, true);

        result.Should().Be(FindResult.Success);

        metadata.Id.Should().Be(1);
        metadata.SeriesName.Should().Be("Show Name");
        metadata.SeasonEpisodes.Should().Be(2);
        metadata.CoverURL.Should().Be("https://image.tmdb.org/t/p/original/poster");
        metadata.Title.Should().Be("S01E02");
        metadata.Overview.Should().Be("Episode two");
        metadata.Genres.Should().BeEquivalentTo("Genre 1");
    }

    [Fact]
    public async Task Should_SetExtendedTags_WhenEnabled()
    {
        var config = new AutoTagConfig
        {
            ExtendedTagging = true
        };
        
        var mockCache = new Mock<ITVCache>();

        var season = new TvSeason
        {
            Episodes =
            [
                new TvSeasonEpisode
                {
                    Name = "S01E01",
                    EpisodeNumber = 1,
                    Overview = "Episode one",
                    Crew = [
                        new Crew
                        {
                            Name = "Director Person",
                            Job = "Director"
                        }
                    ]
                }
            ],
            PosterPath = "poster"
        };
        mockCache.Setup(c => c.TryGetSeason(It.IsAny<int>(), It.IsAny<int>(), out season))
            .Returns(true);

        var mockTmdb = new Mock<ITMDBService>();
        mockTmdb.Setup(tmdb => tmdb.GetTvEpisodeCreditsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new CreditsWithGuestStars
            {
                Cast =
                [
                    new Cast
                    {
                        Name = "Actor Person",
                        Character = "Character Person"
                    },
                    new Cast
                    {
                        Name = "Second Actor Person",
                        Character = "Second Character Person"
                    }
                ]
            });
        

        var metadata = new TVFileMetadata
        {
            Season = 1,
            Episode = 1
        };
        
        var show = new ShowResults(new SearchTv
        {
            Id = 1,
            Name = "Show Name"
        });

        var instance = GetInstance(tmdb: mockTmdb.Object, cache: mockCache.Object, config: config);

        await instance.FindEpisodeAsync(metadata, show, true);

        metadata.Director.Should().Be("Director Person");
        metadata.Actors.Should().BeEquivalentTo("Actor Person", "Second Actor Person");
        metadata.Characters.Should().BeEquivalentTo("Character Person", "Second Character Person");
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Should_NotSetExtendedTags_WhenDisabledOrNotTaggable(bool extendedTagging, bool taggable)
    {
        var config = new AutoTagConfig
        {
            ExtendedTagging = extendedTagging
        };
        
        var mockCache = new Mock<ITVCache>();

        var season = new TvSeason
        {
            Episodes =
            [
                new TvSeasonEpisode
                {
                    Name = "S01E01",
                    EpisodeNumber = 1,
                    Overview = "Episode one",
                    Crew = [
                        new Crew
                        {
                            Name = "A Person",
                            Job = "Director"
                        }
                    ]
                }
            ],
            PosterPath = "poster"
        };
        mockCache.Setup(c => c.TryGetSeason(It.IsAny<int>(), It.IsAny<int>(), out season))
            .Returns(true);

        var metadata = new TVFileMetadata
        {
            Season = 1,
            Episode = 1
        };
        
        var show = new ShowResults(new SearchTv());

        var instance = GetInstance(cache: mockCache.Object, config: config);

        await instance.FindEpisodeAsync(metadata, show, taggable);

        metadata.Director.Should().BeNull();
        metadata.Actors.Should().BeNull();
        metadata.Characters.Should().BeNull();
    }

    [Fact]
    public async Task Should_AddSeasonToCache_When_Found()
    {
        var mockCache = new Mock<ITVCache>();
        TvSeason? cachedSeason = null;
        mockCache.Setup(c => c.TryGetSeason(It.IsAny<int>(), It.IsAny<int>(), out cachedSeason))
            .Returns(false);

        var mockTmdb = new Mock<ITMDBService>();
        var seasonResult = new TvSeason
        {
            Episodes = [new TvSeasonEpisode
            {
                EpisodeNumber = 1
            }]
        };
        mockTmdb.Setup(tmdb => tmdb.GetTvSeasonAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(seasonResult);
        
        var metadata = new TVFileMetadata
        {
            Season = 1,
            Episode = 1
        };
        var show = new ShowResults(new SearchTv
        {
            Id = 1
        });

        var instance = GetInstance(tmdb: mockTmdb.Object, cache: mockCache.Object);

        var (result, _) = await instance.FindEpisodeAsync(metadata, show, true);

        result.Should().Be(FindResult.Success);
        mockCache.Verify(c => c.AddSeason(1, 1, seasonResult));
    }

    [Fact]
    public async Task Should_ReturnSkipWithErrorMessage_When_SeasonNotFound()
    {
        var mockCache = new Mock<ITVCache>();
        TvSeason? cachedSeason = null;
        mockCache.Setup(c => c.TryGetSeason(It.IsAny<int>(), It.IsAny<int>(), out cachedSeason))
            .Returns(false);

        var mockTmdb = new Mock<ITMDBService>();
        mockTmdb.Setup(tmdb => tmdb.GetTvSeasonAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((TvSeason?) null);
        
        var metadata = new TVFileMetadata
        {
            Season = 1,
            Episode = 1
        };
        var show = new ShowResults(new SearchTv());

        var instance = GetInstance(tmdb: mockTmdb.Object, cache: mockCache.Object);

        var (result, msg) = await instance.FindEpisodeAsync(metadata, show, true);

        result.Should().Be(FindResult.Skip);
        msg.Should().Be($"Error: Cannot find {metadata} on TheMovieDB");
    }

    [Fact]
    public async Task Should_ReturnSkipWithErrorMessage_When_EpisodeNotFound()
    {
        var mockCache = new Mock<ITVCache>();
        var cachedSeason = new TvSeason
        {
            Episodes = [new TvSeasonEpisode
            {
                EpisodeNumber = 20
            }]
        };
        mockCache.Setup(c => c.TryGetSeason(It.IsAny<int>(), It.IsAny<int>(), out cachedSeason))
            .Returns(true);
        
        var metadata = new TVFileMetadata
        {
            Season = 1,
            Episode = 1
        };
        var show = new ShowResults(new SearchTv());

        var instance = GetInstance(cache: mockCache.Object);

        var (result, msg) = await instance.FindEpisodeAsync(metadata, show, true);
        
        result.Should().Be(FindResult.Skip);
        msg.Should().Be($"Error: Cannot find {metadata} on TheMovieDB");
    }
    
    [Fact]
    public async Task Should_UseEpisodeGroupMapping_WhenAvailable()
    {
        var mockCache = new Mock<ITVCache>();

        var season = new TvSeason
        {
            Episodes =
            [
                new TvSeasonEpisode
                {
                    Name = "S01E02",
                    EpisodeNumber = 2
                }
            ]
        };
        mockCache.Setup(c => c.TryGetSeason(It.IsAny<int>(), It.IsAny<int>(), out season))
            .Returns(true);
        
        var show = new ShowResults(new SearchTv());
        show.AddEpisodeGroup(
            new TvGroupCollection
            {
                Groups =
                [
                    new TvGroup
                    {
                        Name = "Season 1",
                        Episodes =
                        [
                            new TvGroupEpisode
                            {
                                Order = 0,
                                SeasonNumber = 1,
                                EpisodeNumber = 2
                            }
                        ]
                    }
                ]
            },
            out _
        );

        var metadata = new TVFileMetadata
        {
            Season = 1,
            Episode = 1
        };

        var instance = GetInstance(cache: mockCache.Object);

        var (result, _) = await instance.FindEpisodeAsync(metadata, show, true);

        result.Should().Be(FindResult.Success);
        metadata.Season.Should().Be(1);
        metadata.Episode.Should().Be(1);
        metadata.Title.Should().Be("S01E02");
    }

    [Fact]
    public async Task Should_ReportError_When_EpisodeNotFoundInMapping()
    {
        var mockUi = new Mock<IUserInterface>();
        
        var show = new ShowResults(new SearchTv());
        show.AddEpisodeGroup(
            new TvGroupCollection
            {
                Groups =
                [
                    new TvGroup
                    {
                        Name = "Season 1",
                        Episodes =
                        [
                            new TvGroupEpisode
                            {
                                Order = 0,
                                SeasonNumber = 1,
                                EpisodeNumber = 2
                            }
                        ]
                    }
                ]
            },
            out _
        );
        
        var metadata = new TVFileMetadata
        {
            SeriesName = "Show",
            Season = 10,
            Episode = 1
        };

        var instance = GetInstance(ui: mockUi.Object);

        var (result, _) = await instance.FindEpisodeAsync(metadata, show, true);

        result.Should().Be(FindResult.Fail);
        
        mockUi.Verify(ui => ui.SetStatus($"Error: Cannot find {metadata} in episode group on TheMovieDB", MessageType.Error));
    }
}