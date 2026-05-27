using System.Text.Json;
using AutoTag.CLI.Test.Helpers;
using AutoTag.Core.Config;
using AutoTag.Core.Files;
using AwesomeAssertions.Execution;

namespace AutoTag.CLI.Test;

public class ConfigTests(CLIFixture cli, ITestContextAccessor context) : CLITestBase
{
    [Fact]
    public async Task Should_CreateConfigFile_WhenDoesntExist()
    {
        await cli.ExecuteAsync("--print-config");

        File.Exists(ConfigPath).Should().BeTrue();
    }

    [Fact]
    public async Task Should_SaveArgumentsToConfigFile_WhenSetDefaultArgumentSet()
    {
        await cli.ExecuteAsync(
            "--set-default",
            "--print-config",
            "-p", "parse pattern",
            "--no-rename",
            "--tv-pattern", "{Series} {Season:S00}{Episode:E00}",
            "--movie-pattern", "{Title} {Year}",
            "--windows-safe",
            "--rename-subs",
            "--replace", "a=b",
            "--replace", "cde=fgh",
            "-t",
            "--no-tag",
            "--no-cover",
            "--manual",
            "--extended-tagging",
            "--apple-tagging",
            "-l", "pt-BR",
            "--search-language", "en-GB",
            "--search-language", "en-US",
            "-g",
            "--include-adult",
            "--remove-empty-folders"
        );

        var config = JsonSerializer.Deserialize<AutoTagConfig>(
            await File.ReadAllTextAsync(ConfigPath, context.Current.CancellationToken),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        )!;

        using (new AssertionScope())
        {
            config.ParsePattern.Should().Be("parse pattern");
            config.RenameFiles.Should().BeFalse();
            config.TVRenamePattern.Should().Be("{Series} {Season:S00}{Episode:E00}");
            config.MovieRenamePattern.Should().Be("{Title} {Year}");
            config.WindowsSafe.Should().BeTrue();
            config.RenameSubtitles.Should().BeTrue();
            config.FileNameReplaces.Should()
                .BeEquivalentTo([new FileNameReplace("a", "b"), new FileNameReplace("cde", "fgh")]);
            config.Mode.Should().Be(Mode.TV);
            config.TagFiles.Should().BeFalse();
            config.AddCoverArt.Should().BeFalse();
            config.ManualMode.Should().BeTrue();
            config.ExtendedTagging.Should().BeTrue();
            config.AppleTagging.Should().BeTrue();
            config.Language.Should().Be("pt-BR");
            config.SearchLanguages.Should().BeEquivalentTo("en-GB", "en-US");
            config.EpisodeGroup.Should().BeTrue();
            config.IncludeAdult.Should().BeTrue();
            config.RemoveEmptyFolders.Should().BeTrue();
        }
    }
}