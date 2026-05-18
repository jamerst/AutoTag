using AutoTag.Core.Config;
using AutoTag.Core.Files.Parsing;
using AutoTag.Core.TMDB;
using AutoTag.Core.TV;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;

namespace AutoTag.Core.Test.TV.TVProcessor;

public class FindEpisodeAsync : TVProcessorTestBase
{
    private static ParsedTVFileName GetParsedFileName(int? season, int episode) =>
        new("", null, season, episode, null, null);

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


        var instance = GetInstance(mockTmdb.Object, cache: mockCache.Object);

        var (result, metadata, _) =
            await instance.FindEpisodeAsync(GetParsedFileName(1, 2) with { EndEpisode = 3, Part = 2 }, show, true);

        result.Should().Be(FindResult.Success);

        metadata.Should().NotBeNull();
        metadata.Id.Should().Be(1);
        metadata.SeriesName.Should().Be("Show Name");
        metadata.Season.Should().Be(1);
        metadata.Episode.Should().Be(2);
        metadata.EndEpisode.Should().Be(3);
        metadata.SeasonEpisodes.Should().Be(2);
        metadata.CoverURL.Should().Be("https://image.tmdb.org/t/p/original/poster");
        metadata.Title.Should().Be("S01E02");
        metadata.Overview.Should().Be("Episode two");
        metadata.Genres.Should().BeEquivalentTo("Genre 1");
        metadata.Part.Should().Be(2);
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
                    Crew =
                    [
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

        var show = new ShowResults(new SearchTv
        {
            Id = 1,
            Name = "Show Name"
        });

        var instance = GetInstance(mockTmdb.Object, cache: mockCache.Object, config: config);

        var (_, metadata, _) = await instance.FindEpisodeAsync(GetParsedFileName(1, 1), show, true);

        metadata.Should().NotBeNull();
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
                    Crew =
                    [
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

        var show = new ShowResults(new SearchTv());

        var instance = GetInstance(cache: mockCache.Object, config: config);

        var (_, metadata, _) = await instance.FindEpisodeAsync(GetParsedFileName(1, 1), show, taggable);

        metadata.Should().NotBeNull();
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
            Episodes =
            [
                new TvSeasonEpisode
                {
                    EpisodeNumber = 1
                }
            ]
        };
        mockTmdb.Setup(tmdb => tmdb.GetTvSeasonAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(seasonResult);

        var show = new ShowResults(new SearchTv
        {
            Id = 1
        });

        var instance = GetInstance(mockTmdb.Object, cache: mockCache.Object);

        var (result, _, _) = await instance.FindEpisodeAsync(GetParsedFileName(1, 1), show, true);

        result.Should().Be(FindResult.Success);
        mockCache.Verify(c => c.AddSeason(1, 1, seasonResult));
    }

    [Fact]
    public async Task Should_MapAbsoluteEpisodeToSeasonAndEpisode()
    {
        var mockCache = new Mock<ITVCache>();
        var cachedSeasons = new Dictionary<(int ShowId, int SeasonNumber), TvSeason>();
        mockCache.Setup(c => c.TryGetSeason(It.IsAny<int>(), It.IsAny<int>(), out It.Ref<TvSeason?>.IsAny))
            .Returns((int showId, int seasonNumber, out TvSeason? season) =>
            {
                var found = cachedSeasons.TryGetValue((showId, seasonNumber), out var cachedSeason);
                season = cachedSeason;
                return found;
            });
        mockCache.Setup(c => c.AddSeason(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TvSeason>()))
            .Callback<int, int, TvSeason>((showId, seasonNumber, season) =>
            {
                cachedSeasons[(showId, seasonNumber)] = season;
            });

        var mockTmdb = new Mock<ITMDBService>();
        mockTmdb.Setup(tmdb => tmdb.GetTvSeasonAsync(1, 1))
            .ReturnsAsync(new TvSeason
            {
                SeasonNumber = 1,
                Episodes =
                [
                    new TvSeasonEpisode { EpisodeNumber = 1 },
                    new TvSeasonEpisode { EpisodeNumber = 2 },
                    new TvSeasonEpisode { EpisodeNumber = 3 }
                ]
            });
        mockTmdb.Setup(tmdb => tmdb.GetTvSeasonAsync(1, 2))
            .ReturnsAsync(new TvSeason
            {
                SeasonNumber = 2,
                Episodes =
                [
                    new TvSeasonEpisode
                    {
                        EpisodeNumber = 1,
                        Name = "Episode 4",
                        Overview = "Fourth episode"
                    },
                    new TvSeasonEpisode { EpisodeNumber = 2 }
                ]
            });
        mockTmdb.Setup(tmdb => tmdb.GetTvGenreNamesAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync([]);
        mockTmdb.Setup(tmdb => tmdb.GetTvShowAsync(It.IsAny<int>()))
            .ReturnsAsync(new TvShow { Id = 1, NumberOfSeasons = 4 });

        var show = new ShowResults(new SearchTv
        {
            Id = 1,
            Name = "Series"
        });

        var instance = GetInstance(mockTmdb.Object, cache: mockCache.Object);

        var (result, metadata, _) =
            await instance.FindEpisodeAsync(GetParsedFileName(null, 4) with { SeriesName = "Series" }, show, true);

        result.Should().Be(FindResult.Success);
        metadata.Should().NotBeNull();
        metadata.Season.Should().Be(2);
        metadata.Episode.Should().Be(1);
        metadata.Title.Should().Be("Episode 4");
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
            .ReturnsAsync((TvSeason?)null);

        var parsedDetails = GetParsedFileName(1, 1);

        var show = new ShowResults(new SearchTv());

        var instance = GetInstance(mockTmdb.Object, cache: mockCache.Object);

        var (result, _, msg) = await instance.FindEpisodeAsync(parsedDetails, show, true);

        result.Should().Be(FindResult.Skip);
        msg.Should().Be($"Error: Cannot find {parsedDetails} on TheMovieDB");
    }

    [Fact]
    public async Task Should_ReturnSkipWithErrorMessage_When_EpisodeNotFound()
    {
        var mockCache = new Mock<ITVCache>();
        var cachedSeason = new TvSeason
        {
            Episodes =
            [
                new TvSeasonEpisode
                {
                    EpisodeNumber = 20
                }
            ]
        };
        mockCache.Setup(c => c.TryGetSeason(It.IsAny<int>(), It.IsAny<int>(), out cachedSeason))
            .Returns(true);

        var parsedDetails = GetParsedFileName(1, 1);

        var show = new ShowResults(new SearchTv());

        var instance = GetInstance(cache: mockCache.Object);

        var (result, _, msg) = await instance.FindEpisodeAsync(parsedDetails, show, true);

        result.Should().Be(FindResult.Skip);
        msg.Should().Be($"Error: Cannot find {parsedDetails} on TheMovieDB");
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


        var instance = GetInstance(cache: mockCache.Object);

        var (result, metadata, _) = await instance.FindEpisodeAsync(GetParsedFileName(1, 1), show, true);

        result.Should().Be(FindResult.Success);
        metadata.Should().NotBeNull();
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

        var parsedDetails = GetParsedFileName(10, 1) with { SeriesName = "Show" };

        var instance = GetInstance(ui: mockUi.Object);

        var (result, _, _) = await instance.FindEpisodeAsync(parsedDetails, show, true);

        result.Should().Be(FindResult.Fail);

        mockUi.Verify(ui =>
            ui.SetStatus($"Error: Cannot find {parsedDetails} in episode group on TheMovieDB", MessageType.Error));
    }
}