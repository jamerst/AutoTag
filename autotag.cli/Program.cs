using System.CommandLine;
using System.CommandLine.Invocation;
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

    static async Task<int> Main(string[] args)
    {
        var rootCmd = new RootCommand("Automatically tag and rename media files");
        rootCmd.AddArgument(new Argument<IEnumerable<string>>("paths", "Files or directories to process"));

        var options = CommandLineOptions.GetOptions();
        foreach (var option in options)
        {
            rootCmd.Add(option);
        }

        return await rootCmd.InvokeAsync(args);
        // rootCmd.SetHandle()
    }

    private static async Task HandleAsync(
        IEnumerable<string> paths,
        bool tv,
        bool movie,

        string configPath,

        bool noRename,
        bool noTag,
        bool noCover,

        bool manual,

        string? tvPattern,
        string? moviePattern,
        string? parsePattern,

        bool windowsSafe,

        bool extendedTagging,
        bool appleTagging,
        bool renameSubs,

        string language,

        bool setDefault,

        bool verbose)
    {
        Console.WriteLine($"AutoTag v{Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}");
        Console.WriteLine("https://jtattersall.net");
        /*settings = new AutoTagSettings(configPath);

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
        if (!string.IsNullOrEmpty(language))
        {
            settings.Config.Language = language;
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
        */

    }

    /*private async Task OnExecuteAsync()
    {
        if (version)
        {
            Console.WriteLine(Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
            Environment.Exit(0);
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

    [Option(Description = "Metadata language")]
    public string? language { get; set; }

    [Option(Description = "Enable verbose output mode")]
    private bool verbose { get; set; }

    [Option("--set-default", "Set the current arguments as the default", CommandOptionType.NoValue)]
    private bool setDefault { get; set; }

    [Option("--version", "Print version number and exit", CommandOptionType.NoValue)]
    private bool version { get; set; }
    private string[] RemainingArguments { get; } = null!;
*/

}
