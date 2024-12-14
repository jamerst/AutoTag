using AutoTag.Core.Files;
using AutoTag.Core.TV;

namespace AutoTag.Core.Test.TV.TVProcessor;

public class ParseFileName : TVProcessorTestBase
{
    [Theory]
    [InlineData("Fringe 3x03.mkv", "Fringe", 3, 3)]
    [InlineData("Silo S01E10.mkv", "Silo", 1, 10)]
    [InlineData("Inside.No.9.S09E09.mkv", "Inside No 9", 9, 9)] // dot separated
    [InlineData("the_last_of_us_s01e07.mp4", "The Last of Us", 1, 7)] // underscore separated + lowercase
    [InlineData("Alias.S02E01.The.Enemy.Walks.In.1080p.AMZN.WEB-DL.DDP5.1.H.264-LycanHD.mkv", "Alias", 2, 1)] // scene tags
    [InlineData("Warehouse 13 0x01 Episode Title.mp4", "Warehouse 13", 0, 1)] // season 0
    [InlineData("Doctor Who (2005) - 2x13 - Doomsday (2).mkv", "Doctor Who (2005)", 2, 13)] // symbols in series name
    public void Should_ParseCommonNamingFormats(string fileName, string seriesName, int season, int episode)
    {
        var tv = GetInstance();

        var file = new TaggingFile
        {
            Path = fileName
        };
        var result = tv.ParseFileName(file);
        
        result.Should().BeEquivalentTo(
            new TVFileMetadata
            {
                SeriesName = seriesName,
                Season = season,
                Episode = episode
            },
            o => o.Using<string>(StringComparer.OrdinalIgnoreCase)
        );
    }
    
    [Fact]
    public void Should_ParseFromFullPath_When_ParsePatternProvided()
    {
        var config = new AutoTagConfig
        {
            ParsePattern = @".*/(?<SeriesName>.+)/Season (?<Season>\d+)/S\d+E(?<Episode>\d+)"
        };

        var tv = GetInstance(config: config);

        var file = new TaggingFile
        {
            Path = "/test/a/b/Series Name/Season 2/S02E05 Title.mkv"
        };

        var result = tv.ParseFileName(file);

        result.Should().BeEquivalentTo(new TVFileMetadata
        {
            SeriesName = "Series Name",
            Season = 2,
            Episode = 5
        });
    }

    [Theory]
    [InlineData("S01E05.mkv", "Error: Unable to parse series name from filename")]
    [InlineData("Continuuim E03 Episode Title.mp4", "Error: Unable to parse required information from filename")]
    [InlineData("Utopia S01.mp4", "Error: Unable to parse required information from filename")]
    public void Should_ReportError_When_PartMissingFromFileName(string fileName, string expectedMessage)
    {
        var mockUi = new Mock<IUserInterface>();
        mockUi.Setup(ui => ui.SetStatus(It.IsAny<string>(), It.IsAny<MessageType>()));
        
        var tv = GetInstance(ui: mockUi.Object);

        var file = new TaggingFile
        {
            Path = fileName
        };
        tv.ParseFileName(file);
        
        mockUi.Verify(ui => ui.SetStatus(expectedMessage, MessageType.Error), Times.Once);
    }
    
    [Fact]
    public void Should_ReportError_When_ParsePatternDoesNotMatch()
    {
        var mockUi = new Mock<IUserInterface>();
        mockUi.Setup(ui => ui.SetStatus(It.IsAny<string>(), It.IsAny<MessageType>()));
        
        var config = new AutoTagConfig
        {
            ParsePattern = @".*/(?<SeriesName>.+)/Season (?<Season>\d+)/S\d+E(?<Episode>\d+)"
        };

        var tv = GetInstance(ui: mockUi.Object, config: config);

        var file = new TaggingFile
        {
            Path = "/test/a/b/Series Name/S02/Series Name 2x05 Title.mkv"
        };
        
        tv.ParseFileName(file);

        mockUi.Verify(ui => ui.SetStatus("Error: Unable to parse required information from filename", MessageType.Error),
            Times.Once
        );
    }
}