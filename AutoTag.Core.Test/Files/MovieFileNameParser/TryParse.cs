using AutoTag.Core.Files.Parsing;

namespace AutoTag.Core.Test.Files.MovieFileNameParser;

public class TryParse
{
    private static Core.Files.Parsing.MovieFileNameParser GetInstance() => new();

    [Theory]
    [InlineData("Interstellar.mp4", "Interstellar", null)]
    [InlineData("The Lord of the Rings: The Fellowship of the Ring (2001).m4v",
        "The Lord of the Rings: The Fellowship of the Ring", 2001)]
    [InlineData("Monsters.Inc.2001.1080p.BluRay.REMUX.MULTi[Ben The Men].mkv", "Monsters Inc", 2001)]
    [InlineData("28.Weeks.Later.2007.1080p.BluRay.DTS.x264-IDE.mkv", "28 Weeks Later", 2007)]
    [InlineData(
        "The.Hunger.Games.The.Ballad.of.Songbirds.and.Snakes.2023.1080p.AMZN.WEB-DL.DDP5.1.Atmos.H.264-FLUX.mkv",
        "The Hunger Games The Ballad of Songbirds and Snakes", 2023)]
    public void Should_ParseCommonNamingFormats(string fileName, string title, int? year)
    {
        var parser = GetInstance();

        var success = parser.TryParse(fileName, out var result);

        success.Should().BeTrue();
        result.Should().BeEquivalentTo(new ParsedMovieFileName(title, year),
            o => o.Using<string>(StringComparer.OrdinalIgnoreCase));
    }
}