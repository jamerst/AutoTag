using System.Reflection;
using AutoTag.Core.Files;
using Microsoft.Extensions.DependencyInjection;

namespace AutoTag.CLI;

public class CLIInterface(IServiceProvider serviceProvider, AutoTagConfig config) : IUserInterface
{
    private List<TaggingFile> Files = null!;
    private TaggingFile CurrentFile = null!;

    private bool Success = true;
    private int Warnings;

    public async Task<int> RunAsync(IEnumerable<FileSystemInfo> entries)
    {
        AnsiConsole.WriteLine($"AutoTag v{GetVersion()}");
        AnsiConsole.MarkupLine("[link]https://jtattersall.net[/]");

        Files = TaggingFile.FindTaggingFiles(entries, config, this);

        int fileCount = Files.Count;
        if (fileCount == 0)
        {
            Console.WriteLine("No files found");
            return 1;
        }

        var processor = serviceProvider.GetRequiredService<IProcessor>();
        foreach (var file in Files)
        {
            CurrentFile = file;
            AnsiConsole.MarkupLineInterpolated($"[fuchsia]\n{file.Path}:[/]");

            Success &= await processor.ProcessAsync(file);
        }

        return ReportResults(fileCount);
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
        else
        {
            int failedFiles = Files.Count(f => !f.Success);

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

            Console.ResetColor();
            return 1;
        }
    }

    public void DisplayMessage(string message, MessageType type)
    {
        if (type.IsLog() && !config.Verbose)
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

        AnsiConsole.Write(new Text($"{message}\n", new Style(foreground: colour)));
    }

    public void SetStatus(string status, MessageType type)
    {
        if (type.IsError())
        {
            if (!CurrentFile.Success)
            {
                CurrentFile.Status += Environment.NewLine + status;
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
        SetStatus(config.Verbose ? $"{status} ({ex.GetType().Name}: {ex.Message})" : status, type);
    }

    public int? SelectOption(string message, List<string> options)
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<(int?, string)>()
                .Title($"    [yellow]{Markup.Escape(message)}[/]")
                .PageSize(10)
                .AddChoices([
                    ..options.Select((o, i) => (i, o)),
                    (null, "(Skip file)")
                ])
                .UseConverter(o => $"  {o.Item2}")
                .WrapAround()
                .HighlightStyle(new Style(foreground: Color.Aqua))
        );

        return choice.Item1;
    }

    public static string GetVersion() => Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString(3)!;

    public void SetFilePath(string path)
    {
    }
}