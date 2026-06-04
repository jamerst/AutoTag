using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using AutoTag.CLI.Test.Helpers;
using CliWrap;
using CliWrap.Buffered;
using Spectre.Console.Cli.Testing;

[assembly: AssemblyFixture(typeof(CLIFixture))]
[assembly: CaptureConsole(CaptureOut = true, CaptureError = true)]

namespace AutoTag.CLI.Test.Helpers;

public partial class CLIFixture(ITestContextAccessor context) : IAsyncLifetime
{
    private string _cliPublishOutput = null!;

    [GeneratedRegex(@"\x1b\[[0-9;]*m")]
    private static partial Regex AnsiCodeRegex { get; }

    public async ValueTask InitializeAsync()
    {
        if (Debugger.IsAttached)
        {
            return;
        }

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
            .WithEnvironmentVariables(b => b.Set("TMDB_API_KEY", Environment.GetEnvironmentVariable("TMDB_API_KEY")))
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
        if (context.Current.TestClassInstance is not CLITestBase cliTest)
        {
            throw new InvalidOperationException($"Test class must derive from {nameof(CLITestBase)}");
        }

        string[] args = [..arguments, "-c", cliTest.ConfigPath];

        if (Debugger.IsAttached)
        {
            context.Current.SendDiagnosticMessage("Debugger detected, running CLI in-process");

            var app = new CommandAppTester();
            app.SetDefaultCommand<RootCommand>();

            var appResult = await app.RunAsync(args);

            return (RemoveAnsiColourCodes(appResult.Output), appResult.ExitCode);
        }

        if (string.IsNullOrEmpty(_cliPublishOutput))
        {
            throw new InvalidOperationException("CLI publish output not set");
        }

        var cmd = Cli.Wrap(_cliPublishOutput)
            .WithArguments(args)
            .WithValidation(CommandResultValidation.None);

        var result = await cmd.ExecuteBufferedAsync();

        if (!result.IsSuccess)
        {
            context.Current.SendDiagnosticMessage($"CLI returned non-zero exit code ({result.ExitCode})");
            context.Current.SendDiagnosticMessage(result.StandardOutput);
        }

        return (RemoveAnsiColourCodes(result.StandardOutput), result.ExitCode);
    }

    private static string RemoveAnsiColourCodes(string input) => AnsiCodeRegex.Replace(input, "");
}