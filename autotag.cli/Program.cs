using System.Reflection;

using McMaster.Extensions.CommandLineUtils;

using autotag.Core;
using autotag.Core.Movie;
using autotag.Core.TV;

namespace autotag.cli;
[Command(UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.CollectAndContinue)]
class Program
{
    private int warnings = 0;
    private bool success = true;
    private AutoTagSettings settings = null!;

    private async Task OnExecuteAsync()
    {
        if (version)
        {
            Console.WriteLine(Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
            Environment.Exit(0);
        }

        Console.WriteLine($"AutoTag v{Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}");
        Console.WriteLine("https://jtattersall.net");
        settings = new AutoTagSettings(configPath);

        if (tv)
        {
            settings.Config.Mode = AutoTagConfig.Modes.TV;
        }
        else if (movie)
        {
            settings.Config.Mode = AutoTagConfig.Modes.Movie;
        }
        if (noRename)
        {
            settings.Config.RenameFiles = false;
        }
        if (noTag)
        {
            settings.Config.TagFiles = false;
        }
        if (noCoverArt)
        {
            settings.Config.AddCoverArt = false;
        }
        if (manualMode)
        {
            settings.Config.ManualMode = true;
        }
        if (!string.IsNullOrEmpty(tvRenamePattern))
        {
            settings.Config.TVRenamePattern = tvRenamePattern;
        }
        if (!string.IsNullOrEmpty(movieRenamePattern))
        {
            settings.Config.MovieRenamePattern = movieRenamePattern;
        }
        if (!string.IsNullOrEmpty(pattern))
        {
            settings.Config.ParsePattern = pattern;
        }
        if (windowsSafe)
        {
            settings.Config.WindowsSafe = true;
        }
        if (extendedTagging)
        {
            settings.Config.ExtendedTagging = true;
        }
        if (appleTagging)
        {
            settings.Config.AppleTagging = true;
        }
        if (renameSubtitles)
        {
            settings.Config.RenameSubtitles = true;
        }
        if (verbose)
        {
            settings.Config.Verbose = true;
        }
        if (setDefault)
        {
            settings.Save();
        }

        if (!RemainingArguments.Any())
        {
            Console.Error.WriteLine("No files provided");
            Environment.Exit(1);
        }

        IEnumerable<string> supportedExtensions = videoExtensions;
        if (settings.Config.RenameSubtitles)
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
        using (IProcessor processor = settings.Config.IsTVMode() ? new TVProcessor(Keys.TMDBKey) : new MovieProcessor(Keys.TMDBKey))
        {
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

    static void Main(string[] args) => CommandLineApplication.Execute<Program>(args);

    [Option(Description = "TV tagging mode")]
    private bool tv { get; set; }

    [Option(Description = "Movie tagging mode")]
    private bool movie { get; set; }

    [Option(Description = "Specify config file to load")]
    private string configPath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "autotag",
        "conf.json"
    );

    [Option("--no-rename", "Disable file and subtitle renaming", CommandOptionType.NoValue)]
    private bool noRename { get; set; }

    [Option("--no-tag", "Disable file tagging", CommandOptionType.NoValue)]
    private bool noTag { get; set; }

    [Option("--no-cover", "Disable cover art tagging", CommandOptionType.NoValue)]
    private bool noCoverArt { get; set; }

    [Option("--manual", "Manually choose the series to tag from search results", CommandOptionType.NoValue)]
    private bool manualMode { get; set; }

    [Option("--tv-pattern <PATTERN>", "Rename pattern for TV episodes", CommandOptionType.SingleValue)]
    private string tvRenamePattern { get; set; } = "";

    [Option("--movie-pattern <PATTERN>", "Rename pattern for movies", CommandOptionType.SingleValue)]
    private string movieRenamePattern { get; set; } = "";

    [Option(Description = "Custom regex to parse TV episode information")]
    private string pattern { get; set; } = "";

    [Option("--windows-safe", "Remove invalid Windows file name characters when renaming", CommandOptionType.NoValue)]
    private bool windowsSafe { get; set; }

    [Option("--extended-tagging", "Add more information to Matroska file tags. Reduces tagging speed.", CommandOptionType.NoValue)]
    private bool extendedTagging { get; set; }

    [Option("--apple-tagging", "Add extra tags to mp4 files for use with Apple devices and software", CommandOptionType.NoValue)]
    private bool appleTagging { get; set; }

    [Option("--rename-subs", "Rename subtitle files", CommandOptionType.NoValue)]
    private bool renameSubtitles { get; set; }

    [Option(Description = "Enable verbose output mode")]
    private bool verbose { get; set; }

    [Option("--set-default", "Set the current arguments as the default", CommandOptionType.NoValue)]
    private bool setDefault { get; set; }

    [Option("--version", "Print version number and exit", CommandOptionType.NoValue)]
    private bool version { get; set; }
    private string[] RemainingArguments { get; } = null!;

    private static readonly string[] videoExtensions = { ".mp4", ".m4v", ".mkv" };
    private static readonly string[] subtitleExtensions = { ".srt", ".vtt", ".sub", ".ssa" };
}
