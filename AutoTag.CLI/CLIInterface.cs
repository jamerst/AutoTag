using System.Reflection;
using AutoTag.Core.Config;
using AutoTag.Core.Files;
using Microsoft.Extensions.DependencyInjection;

namespace AutoTag.CLI;

public class CLIInterface(IServiceProvider serviceProvider) : IUserInterface
{
    private AutoTagConfig Config = null!;
    private TaggingFile CurrentFile = null!;
    private List<TaggingFile> Files = null!;

    private bool Success = true;
    private int Warnings;

    public void DisplayMessage(string message, MessageType type)
    {
        if (type.IsLog() && !Config.Verbose)
        {
            return;
        }

        Color? colour = null;
        if (type.IsError())
        {
            colour = Color.Red;
        }
        else if (type.IsWarning())
        {
            colour = Color.Yellow;
        }

        AnsiConsole.Write(new Text($"{message}\n", new Style(colour)));
    }

    public void SetStatus(string status, MessageType type)
    {
        if (type.IsError())
        {
            if (!CurrentFile.Success)
            {
                CurrentFile.Status += $"{Environment.NewLine}    {status}";
            }
            else
            {
                CurrentFile.Status = status;
            }

            Success = false;
            CurrentFile.Success = false;
        }
        else if (CurrentFile.Success)
        {
            CurrentFile.Status = status;
        }

        if (type.IsWarning())
        {
            Warnings++;
        }

        DisplayMessage($"    {status}", type);
    }

    public void SetStatus(string status, MessageType type, Exception ex)
    {
        SetStatus(status, type);

        if (Config.Verbose)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
        }
    }

    public int? SelectOption(string message, List<string> options)
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<(int?, string)>()
                .Title($"    [yellow]{Markup.Escape(message)}[/]")
                .PageSize(10)
                .AddChoices([
                    ..options.Select((o, i) => (i, Markup.Escape(o))),
                    (null, "(Skip file)")
                ])
                .UseConverter(o => $"  {o.Item2}")
                .WrapAround()
                .HighlightStyle(new Style(Color.Aqua))
        );

        return choice.Item1;
    }

    public void SetFilePath(string path)
    {
    }

    public async Task<int> RunAsync(IEnumerable<FileSystemInfo> entries)
    {
        Config = serviceProvider.GetRequiredService<AutoTagConfig>();
        var movieProcessor = serviceProvider.GetRequiredKeyedService<IProcessor>(Mode.Movie);
        var tvProcessor = serviceProvider.GetRequiredKeyedService<IProcessor>(Mode.TV);
        var fileFinder = serviceProvider.GetRequiredService<IFileFinder>();

        AnsiConsole.WriteLine($"AutoTag v{GetVersion()}");
        AnsiConsole.MarkupLine("[link]https://jtattersall.net[/]");

        Files = fileFinder.FindFilesToProcess(entries);

        if (Files.Count == 0)
        {
            DisplayMessage("No files found", MessageType.Error);
            return 1;
        }

        foreach (var file in Files)
        {
            CurrentFile = file;
            AnsiConsole.MarkupLineInterpolated($"[fuchsia]\n{file.Path}:[/]");

            Success &= (await ProcessWithFallbackAsync(file, movieProcessor, tvProcessor)).IsSuccess();
        }

        return ReportResults(Files.Count);
    }

    private int ReportResults(int fileCount)
    {
        if (Success)
        {
            if (Warnings == 0)
            {
                AnsiConsole.MarkupLineInterpolated(
                    $"\n\n[green]{(fileCount > 1 ? $"All {fileCount} files" : "File")} successfully processed.[/]");
            }
            else
            {
                AnsiConsole.MarkupLineInterpolated(
                    $"[yellow]\n\n{(fileCount > 1 ? $"All {fileCount} files" : "File")} successfully processed with {Warnings} warning{(Warnings > 1 ? "s" : "")}.[/]");
            }

            return 0;
        }

        var failedFiles = Files.Count(f => !f.Success);

        if (failedFiles < fileCount)
        {
            if (Warnings == 0)
            {
                AnsiConsole.MarkupLineInterpolated(
                    $"[green]\n\n{fileCount - failedFiles} file{(fileCount - failedFiles > 1 ? "s" : "")} successfully processed.[/]");
            }
            else
            {
                AnsiConsole.MarkupLineInterpolated(
                    $"[yellow]\n\n{fileCount - failedFiles} file{(fileCount - failedFiles > 1 ? "s" : "")} successfully processed with {Warnings} warning{(Warnings > 1 ? "s" : "")}.[/]");
            }

            AnsiConsole.MarkupLineInterpolated(
                $"[maroon]Errors encountered for {failedFiles} file{(failedFiles > 1 ? "s" : "")}:[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[maroon]\n\nErrors encountered for all files:[/]");
        }

        foreach (var file in Files.Where(f => !f.Success))
        {
            AnsiConsole.MarkupLineInterpolated($"[magenta]{file.Path}:[/]");
            AnsiConsole.MarkupLineInterpolated($"[red]    {file.Status}\n[/]");
        }

        return 1;
    }

    public static string GetVersion()
    {
        return Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString(3)!;
    }

    private async Task<ProcessResult> ProcessWithFallbackAsync(TaggingFile file, IProcessor movieProcessor,
        IProcessor tvProcessor)
    {
        if (file is { TVDetails: null, MovieDetails: null })
        {
            DisplayMessage("Error: Unable to parse required information from filename", MessageType.Error);
            return ProcessResult.ParseFailure;
        }

        if (file.TVDetails is not null)
        {
            var result = await tvProcessor.ProcessAsync(file);

            if (result != ProcessResult.NotFound)
            {
                return result;
            }
        }

        if (file.MovieDetails is not null)
        {
            return await movieProcessor.ProcessAsync(file);
        }

        return ProcessResult.Fail;
    }
}