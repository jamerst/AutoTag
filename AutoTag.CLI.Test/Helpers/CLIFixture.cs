using System.Text;
using AutoTag.CLI.Test.Helpers;
using CliWrap;

[assembly: AssemblyFixture(typeof(CLIFixture))]
[assembly: CaptureConsole(CaptureOut = true, CaptureError = true)]

namespace AutoTag.CLI.Test.Helpers;

public class CLIFixture(ITestContextAccessor context) : IAsyncLifetime
{
    private string _cliPublishOutput = null!;

    public async ValueTask InitializeAsync()
    {
        var stdout = new StringBuilder();

        string? outputPath = null;
        var build = await Cli.Wrap("dotnet")
            .WithWorkingDirectory(Path.Combine("..", "..", "..", ".."))
            .WithArguments(["publish", "AutoTag.CLI", "-c", "Release"])
            .WithStandardOutputPipe(PipeTarget.ToDelegate(l =>
            {
                stdout.AppendLine(l);

                if (l.Contains("AutoTag.CLI ->"))
                {
                    outputPath = Path.Combine(
                        l.Split("->", 2, StringSplitOptions.TrimEntries)[1],
                        $"autotag{(OperatingSystem.IsWindows() ? ".exe" : "")}"
                    );
                }
            }))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(context.Current.SendDiagnosticMessage))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();

        if (!build.IsSuccess || string.IsNullOrEmpty(outputPath))
        {
            context.Current.SendDiagnosticMessage("CLI production build failed:\n{0}", stdout.ToString());

            throw new Exception("CLI build failed");
        }

        _cliPublishOutput = outputPath;
    }

    public ValueTask DisposeAsync()
    {
        _cliPublishOutput = "";

        return ValueTask.CompletedTask;
    }

    public async Task<(string, int)> ExecuteAsync(params string[] arguments)
    {
        if (string.IsNullOrEmpty(_cliPublishOutput))
        {
            throw new InvalidOperationException("CLI publish output not set");
        }

        if (context.Current.TestClassInstance is not CLITestBase cliTest)
        {
            throw new InvalidOperationException($"Test class must derive from {nameof(CLITestBase)}");
        }

        var stdout = new StringBuilder();
        var cmd = Cli.Wrap(_cliPublishOutput)
            .WithArguments([..arguments, "-c", cliTest.ConfigPath])
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdout))
            .WithValidation(CommandResultValidation.None);

        var result = await cmd.ExecuteAsync();

        return (stdout.ToString(), result.ExitCode);
    }
}