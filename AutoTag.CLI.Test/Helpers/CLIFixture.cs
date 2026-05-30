using System.Diagnostics;
using System.Text;
using AutoTag.CLI.Test.Helpers;
using CliWrap;
using CliWrap.Buffered;
using Spectre.Console.Cli.Testing;

[assembly: AssemblyFixture(typeof(CLIFixture))]
[assembly: CaptureConsole(CaptureOut = true, CaptureError = true)]

namespace AutoTag.CLI.Test.Helpers;

public class CLIFixture(ITestContextAccessor context) : IAsyncLifetime
{
    private string _cliPublishOutput = null!;

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

            return (appResult.Output, appResult.ExitCode);
        }

        if (string.IsNullOrEmpty(_cliPublishOutput))
        {
            throw new InvalidOperationException("CLI publish output not set");
        }

        var cmd = Cli.Wrap(_cliPublishOutput)
            .WithArguments(args)
            .WithValidation(CommandResultValidation.None)
            .WithEnvironmentVariables(env => env.Set("TERM", "dumb"));

        var result = await cmd.ExecuteBufferedAsync();

        return (result.StandardOutput, result.ExitCode);
    }
}