using autotag.Core;
using autotag.Core.Movie;
using autotag.Core.TV;

namespace autotag.cli;

public class Processor
{
    /*public async Task ProcessAsync(AutoTagConfig config)
    {
        IEnumerable<string> supportedExtensions = videoExtensions;
        if (config.RenameSubtitles)
        {
            supportedExtensions = supportedExtensions.Concat(subtitleExtensions);
        }

        var tempFiles = FindFiles(RemainingArguments, supportedExtensions)
            .DistinctBy(f => f.Path);

        if (settings.Config.RenameSubtitles && string.IsNullOrEmpty(settings.Config.ParsePattern))
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
            Console.Error.WriteLine("No files found");
            Environment.Exit(1);
        }

        using (FileWriter writer = new FileWriter())
        using (IProcessor processor = settings.Config.IsTVMode()
            ? new TVProcessor(Keys.TMDBKey, settings.Config)
            : new MovieProcessor(Keys.TMDBKey, settings.Config)
        ) {
            foreach (var file in files)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"\n{file.Path}:");
                Console.ResetColor();

                success &= await processor.ProcessAsync(file, p => { }, (s, t) => SetStatus(file, s, t), ChooseResult, settings.Config, writer);
            }
        }

        Console.ResetColor();

        if (success)
        {
            if (warnings == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n\n{(fileCount > 1 ? $"All {fileCount} files" : "File")} successfully processed.");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n\n{(fileCount > 1 ? $"All {fileCount} files" : "File")} successfully processed with {warnings} warning{(warnings > 1 ? "s" : "")}.");
            }
            Console.ResetColor();
            Environment.Exit(0);
        }
        else
        {
            int failedFiles = files.Count(f => !f.Success);

            if (failedFiles < fileCount)
            {
                if (warnings == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n\n{fileCount - failedFiles} file{(fileCount - failedFiles > 1 ? "s" : "")} successfully processed.");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\n\n{fileCount - failedFiles} file{(fileCount - failedFiles > 1 ? "s" : "")} successfully processed with {warnings} warning{(warnings > 1 ? "s" : "")}.");
                }

                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.Error.WriteLine($"Errors encountered for {failedFiles} file{(failedFiles > 1 ? "s" : "")}:");
            }
            else
            {
                Console.Error.WriteLine("\n\nErrors encountered for all files:");
            }

            foreach (TaggingFile file in files.Where(f => !f.Success))
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Error.WriteLine($"{file.Path}:");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"    {file.Status}\n");
            }

            Console.ResetColor();
            Environment.Exit(1);
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
            success = false;
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
            warnings++;
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

    private IEnumerable<TaggingFile> FindFiles(IEnumerable<string> paths, IEnumerable<string> supportedExtensions)
    {
        foreach (string path in paths)
        {
            if (File.Exists(path) || Directory.Exists(path))
            {
                if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
                {
                    if (settings.Config.Verbose)
                    {
                        Console.WriteLine($"Adding all files in directory '{path}'");
                    }

                    foreach (var file in FindFiles(Directory.GetFileSystemEntries(path), supportedExtensions))
                    {
                        yield return file;
                    }
                }
                else if (supportedExtensions.Contains(Path.GetExtension(path)))
                {
                    // add file if not already added and has a supported file extension
                    if (settings.Config.Verbose)
                    {
                        Console.WriteLine($"Adding file '{path}'");
                    }

                    yield return new TaggingFile
                    {
                        Path = path,
                        Taggable = videoExtensions.Contains(Path.GetExtension(path))
                    };
                }
                else if (settings.Config.Verbose)
                {
                    Console.Error.WriteLine($"Unsupported file: '{path}'");
                }
            }
            else
            {
                Console.Error.WriteLine($"Path not found: {path}");
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
            if (settings.Config.Verbose)
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
    private static readonly string[] subtitleExtensions = { ".srt", ".vtt", ".sub", ".ssa" };*/
}