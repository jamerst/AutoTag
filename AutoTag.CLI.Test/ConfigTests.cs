using AutoTag.CLI.Test.Helpers;
using FluentAssertions;

namespace AutoTag.CLI.Test;

public class ConfigTests(CLIFixture cli)
{
    [Fact]
    public async Task Should_CreateConfigFile_WhenDoesntExist()
    {
        var configPath = Path.Combine(".", "test-config.json");

        await cli.ExecuteAsync(c => c.WithArguments(["-c", configPath, "--print-config"]));

        File.Exists(configPath).Should().BeTrue();
    }
}