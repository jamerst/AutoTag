using System.Reflection;
using AutoTag.Core.Config;
using AutoTag.Core.Files;
using Microsoft.Extensions.DependencyInjection;

namespace AutoTag.CLI;

public class CLIInterface(IServiceProvider serviceProvider, IAnsiConsole console) : IUserInterface
{
    private AutoTagConfig Config = null!;
    private TaggingFile CurrentFile = null!;

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

        console.Write(new Text($"{message}\n", new Style(colour)));
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
        }
        else if (CurrentFile.Success)
        {
            CurrentFile.Status = status;
        }

        CurrentFile.HasWarnings |= type.IsWarning();

        DisplayMessage($"    {status}", type);
    }

    public void SetStatus(string status, MessageType type, Exception ex)
    {
        SetStatus(status, type);

        if (Config.Verbose)
        {
            console.WriteException(ex, ExceptionFormats.ShortenEverything);
        }
    }

    public int? SelectOption(string message, List<string> options)
    {
        var choice = console.Prompt(
            new SelectionPrompt<(int? Index, string Value)>()
                .Title($"    [yellow]{Markup.Escape(message)}[/]")
                .PageSize(10)
                .AddChoices([
                    ..options.Select((o, i) => (i, Markup.Escape(o))),
                    (null, "(Skip file)")
                ])
                .UseConverter(o => $"  {o.Value}")
                .WrapAround()
                .HighlightStyle(new Style(Color.Aqua))
        );

        return choice.Index;
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

        console.WriteLine($"AutoTag v{GetVersion()}");
        console.MarkupLine("[link]https://jtattersall.net[/]");

        var files = fileFinder.FindFilesToProcess(entries);

        if (files.Count == 0)
        {
            DisplayMessage("No files found", MessageType.Error);
            return 1;
        }

        foreach (var file in files)
        {
            CurrentFile = file;
            console.MarkupLineInterpolated($"[fuchsia]\n{file.Path}:[/]");

            file.Success = (await ProcessWithFallbackAsync(file, movieProcessor, tvProcessor)).IsSuccess();
        }

        return ReportResults(files);
    }

    private int ReportResults(List<TaggingFile> files)
    {
        var succeeded = files.Count(f => f.Success);
        var warnings = files.Count(f => f.HasWarnings);
        var failed = files.Count - succeeded;

        if (succeeded == 0)
        {
            console.MarkupLine("[maroon]\n\nErrors encountered for all files:[/]");
        }
        else
        {
            var succeededFiles = $"{succeeded} files";
            if (failed == 0)
            {
                succeededFiles = files.Count == 1 ? "File" : "All files";
            }

            if (warnings == 0)
            {
                console.MarkupLineInterpolated(
                    $"[green]\n\n{succeededFiles} successfully processed.[/]");
            }
            else
            {
                console.MarkupLineInterpolated(
                    $"[yellow]\n\n{succeededFiles} successfully processed with {warnings} warning{(warnings > 1 ? "s" : "")}.[/]");
            }

            if (failed == 0)
            {
                return 0;
            }

            console.MarkupLineInterpolated(
                $"[maroon]Errors encountered for {failed} file{(failed > 1 ? "s" : "")}:[/]");
        }

        foreach (var file in files.Where(f => !f.Success))
        {
            console.MarkupLineInterpolated($"[magenta]{file.Path}:[/]");
            console.MarkupLineInterpolated($"[red]    {file.Status}\n[/]");
        }

        return 1;
    }

    public static string GetVersion() => Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString(3)!;

    private async Task<ProcessResult> ProcessWithFallbackAsync(TaggingFile file, IProcessor movieProcessor,
        IProcessor tvProcessor)
    {
        if (file is { TVDetails: null, MovieDetails: null })
        {
            SetStatus("Error: Unable to parse required information from filename", MessageType.Error);
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