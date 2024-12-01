using AutoTag.Core.Movie;
using AutoTag.Core.TV;

namespace AutoTag.CLI;

public class Processor
{
    private readonly AutoTagConfig Config;
    private bool Success = true;
    private int Warnings = 0;

    public Processor(AutoTagConfig config)
    {
        Config = config;
    }

    public async Task<int> ProcessAsync(IEnumerable<FileSystemInfo> entries)
    {
        IEnumerable<string> supportedExtensions = videoExtensions;
        if (Config.RenameSubtitles)
        {
            supportedExtensions = supportedExtensions.Concat(subtitleExtensions);
        }

        var tempFiles = FindFiles(entries, supportedExtensions)
            .DistinctBy(f => f.Path);

        if (Config.RenameSubtitles && string.IsNullOrEmpty(Config.ParsePattern))
        {
            tempFiles = tempFiles
                .GroupBy(f => Path.GetFileNameWithoutExtension(f.Path))
                .SelectMany(f => FindSubtitles(f));
        }

        List<TaggingFile> files = tempFiles
            .OrderBy(f => f.Path)
            .ToList();

        int fileCount = files.Count();

        if (!files.Any())
        {
            Console.WriteLine("No files found");
            return 1;
        }
        
        var ui = new CLIInterface();

        using (FileWriter writer = new FileWriter(Config))
        using (IProcessor processor = Config.IsTVMode()
            ? new TVProcessor(Keys.TMDBKey, Config)
            : new MovieProcessor(Keys.TMDBKey, Config)
        ) {
            foreach (var file in files)
            {
                ui.SetCurrentFile(file);
                
                AnsiConsole.MarkupLineInterpolated($"[magenta]\n{file.Path}:[/]");

                Success &= await processor.ProcessAsync(file, writer, ui);
            }
        }

        Console.ResetColor();

        if (Success)
        {
            if (Warnings == 0)
            {
                AnsiConsole.MarkupLineInterpolated($"\n\n[green]{(fileCount > 1 ? $"All {fileCount} files" : "File")} successfully processed.[/]");
            }
            else
            {
                AnsiConsole.MarkupLineInterpolated($"[yellow]\n\n{(fileCount > 1 ? $"All {fileCount} files" : "File")} successfully processed with {Warnings} warning{(Warnings > 1 ? "s" : "")}.[/]");
            }

            return 0;
        }
        else
        {
            int failedFiles = files.Count(f => !f.Success);

            if (failedFiles < fileCount)
            {
                if (Warnings == 0)
                {
                    AnsiConsole.MarkupLineInterpolated($"[green]\n\n{fileCount - failedFiles} file{(fileCount - failedFiles > 1 ? "s" : "")} successfully processed.[/]");
                }
                else
                {
                    AnsiConsole.MarkupLineInterpolated($"[yellow]\n\n{fileCount - failedFiles} file{(fileCount - failedFiles > 1 ? "s" : "")} successfully processed with {Warnings} warning{(Warnings > 1 ? "s" : "")}.[/]");
                }
                
                AnsiConsole.MarkupLineInterpolated($"[maroon]Errors encountered for {failedFiles} file{(failedFiles > 1 ? "s" : "")}:[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[maroon]\n\nErrors encountered for all files:[/]");
            }

            foreach (var file in files.Where(f => !f.Success))
            {
                AnsiConsole.MarkupLineInterpolated($"[magenta]{file.Path}:[/]");
                AnsiConsole.MarkupLineInterpolated($"[red]    {file.Status}\n[/]");
            }

            Console.ResetColor();
            return 1;
        }
    }

    private void SetStatus(TaggingFile file, string status, MessageType type)
    {
        if (type == MessageType.Error && !file.Success)
        {
            file.Status += Environment.NewLine + status;
        }
        else if (type == MessageType.Error)
        {
            Success = false;
            file.Success = false;
            Console.ForegroundColor = ConsoleColor.Red;
            file.Status = status;
        }
        else if (file.Success)
        {
            file.Status = status;
        }

        if (type == MessageType.Warning)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Warnings++;
        }

        Console.WriteLine($"    {file.Status}");
        Console.ResetColor();
    }

    private int? ChooseResult(List<(string, string)> results)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("    Please choose an option, or press enter to skip file:");
        Console.ResetColor();
        for (int i = 0; i < results.Count; i++)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"        {i}: {results[i].Item1} ({results[i].Item2})");
        }
        Console.ResetColor();

        int? choice = null;
        bool inputSuccess = false;
        while (!inputSuccess)
        {
            choice = InputResult(results.Count, out inputSuccess);
        }

        return choice;
    }

    private int? InputResult(int count, out bool success)
    {
        success = true;
        Console.Write($"    Choose an option [0-{count - 1}]: ");
        string? choice = Console.ReadLine();

        int chosen;
        if (int.TryParse(choice, out chosen) && chosen >= 0 && chosen < count)
        {
            return chosen;
        }
        else if (!string.IsNullOrEmpty(choice))
        {
            // if entry is either not a number or out of range
            success = false;
        }

        return null;
    }

    private IEnumerable<TaggingFile> FindFiles(IEnumerable<FileSystemInfo> entries, IEnumerable<string> supportedExtensions)
    {
        foreach (var entry in entries)
        {
            if (entry.Exists)
            {
                if (entry is DirectoryInfo directory)
                {
                    if (Config.Verbose)
                    {
                        Console.WriteLine($"Adding all files in directory '{directory}'");
                    }

                    foreach (var file in FindFiles(directory.GetFileSystemInfos(), supportedExtensions))
                    {
                        yield return file;
                    }
                }
                else if (entry is FileInfo file && supportedExtensions.Contains(file.Extension))
                {
                    // add file if not already added and has a supported file extension
                    if (Config.Verbose)
                    {
                        Console.WriteLine($"Adding file '{file}'");
                    }

                    yield return new TaggingFile
                    {
                        Path = file.FullName,
                        Taggable = videoExtensions.Contains(file.Extension)
                    };
                }
                else if (Config.Verbose)
                {
                    Console.Error.WriteLine($"Unsupported file: '{entry}'");
                }
            }
            else
            {
                Console.Error.WriteLine($"Path not found: {entry}");
            }
        }
    }

    public IEnumerable<TaggingFile> FindSubtitles(IGrouping<string, TaggingFile> files)
    {
        if (files.Count() == 1)
        {
            yield return files.First();
        }
        else if (files.Count(f => videoExtensions.Contains(Path.GetExtension(f.Path))) > 1
            || files.Count(f => subtitleExtensions.Contains(Path.GetExtension(f.Path))) > 1)
        {
            if (Config.Verbose)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($@"Warning, detected multiple files named ""{files.Key}"", files will be processed separately");
                Console.ResetColor();
            }

            foreach (var f in files)
            {
                yield return f;
            }
        }
        else
        {
            string? videoPath = files.FirstOrDefault(f => videoExtensions.Contains(Path.GetExtension(f.Path)))?.Path;
            string? subPath = files.FirstOrDefault(f => subtitleExtensions.Contains(Path.GetExtension(f.Path)))?.Path;

            if (videoPath != null)
            {
                yield return new TaggingFile
                {
                    Path = videoPath,
                    SubtitlePath = subPath
                };
            }
            else if (subPath != null)
            {
                yield return new TaggingFile
                {
                    Path = subPath,
                    Taggable = false
                };
            }
        }
    }

    private static readonly string[] videoExtensions = { ".mp4", ".m4v", ".mkv" };
    private static readonly string[] subtitleExtensions = { ".srt", ".vtt", ".sub", ".ssa" };
}