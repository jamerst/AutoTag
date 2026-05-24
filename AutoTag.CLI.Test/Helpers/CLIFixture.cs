using System.Text;
using AutoTag.CLI.Test.Helpers;
using CliWrap;

[assembly: AssemblyFixture(typeof(CLIFixture))]
[assembly: CaptureConsole]

namespace AutoTag.CLI.Test.Helpers;

public class CLIFixture : IAsyncLifetime
{
    private string _cliPublishOutput = null!;

    public async ValueTask InitializeAsync()
    {
        var wd = Directory.GetCurrentDirectory();

        string? outputPath = null;
        var build = await Cli.Wrap("dotnet")
            .WithWorkingDirectory(Path.Combine("..", "..", "..", ".."))
            .WithArguments(["publish", "AutoTag.CLI", "-c", "Release"])
            .WithStandardOutputPipe(PipeTarget.ToDelegate(l =>
            {
                Console.WriteLine(l);
                if (l.Contains("AutoTag.CLI ->"))
                {
                    outputPath = Path.Combine(
                        l.Split("->", 2, StringSplitOptions.TrimEntries)[1],
                        $"autotag{(OperatingSystem.IsWindows() ? ".exe" : "")}"
                    );
                }
            }))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(Console.WriteLine))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();

        if (!build.IsSuccess || string.IsNullOrEmpty(outputPath))
        {
            throw new Exception("CLI build failed");
        }

        _cliPublishOutput = outputPath;
    }

    public ValueTask DisposeAsync()
    {
        _cliPublishOutput = "";

        return ValueTask.CompletedTask;
    }

    public async Task<(string, int)> ExecuteAsync(Func<Command, Command> configure)
    {
        if (string.IsNullOrEmpty(_cliPublishOutput))
        {
            throw new InvalidOperationException("CLI publish output not set");
        }

        var stdout = new StringBuilder();

        var cmd = configure(Cli.Wrap(_cliPublishOutput)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdout))
            .WithValidation(CommandResultValidation.None));

        var result = await cmd.ExecuteAsync();

        return (stdout.ToString(), result.ExitCode);
    }
}