using System.CommandLine;

namespace autotag.cli;

public class CommandLineOptions
{
    [CommandLineOption<bool>("--tv", "-t", "TV tagging mode")]
    public bool TVMode { get; set; }

    [CommandLineOption<bool>("--movie", "-m", "Movie tagging mode")]
    public bool MovieMode { get; set; }

    [CommandLineOption<string>("--config", "-c", "Config file path")]
    public string ConfigPath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "autotag",
        "conf.json"
    );

    [CommandLineOption<bool>("--no-rename", Description = "Disable file and subtitle renaming")]
    public bool NoRename { get; set; }
    [CommandLineOption<bool>("--no-tag", Description = "Disable file tagging")]
    public bool NoTag { get; set; }
    [CommandLineOption<bool>("--no-cover", Description = "Disable cover art tagging")]
    public bool NoCover { get; set; }

    [CommandLineOption<bool>("--manual", Description = "Manually choose the TV series for a file from search results")]
    public bool Manual { get; set; }

    [CommandLineOption<string>("--tv-pattern", Description = "Rename pattern for TV episodes")]
    public string? TVPattern { get; set; }
    [CommandLineOption<string>("--movie-pattern", Description = "Rename pattern for movies")]
    public string? MoviePattern { get; set; }

    [CommandLineOption<string>("--pattern", "-p", "Custom regex to parse TV episode information")]
    public string? ParsePattern { get; set; }

    [CommandLineOption<bool>("--windows-safe", Description = "Remove invalid Windows file name characters when renaming")]
    public bool WindowsSafe { get; set; }

    [CommandLineOption<bool>("--extended-tagging", Description = "Add more information to Matroska file tags. Reduces tagging speed.")]
    public bool ExtendedTagging { get; set; }
    [CommandLineOption<bool>("--apple-tagging", Description = "Add extra tags to mp4 files for use with Apple devices and software")]
    public bool AppleTagging { get; set; }
    [CommandLineOption<bool>("--rename-subs", Description = "Rename subtitle files")]
    public bool RenameSubs { get; set; }

    [CommandLineOption<bool>("--language", "-l", "Metadata language")]
    public string Language { get; set; } = "en";

    [CommandLineOption<bool>("--set-default", Description = "Set the current arguments as the default")]
    public bool SetDefault { get; set; }

    [CommandLineOption<bool>("--verbose", "-v", "Enable verbose output mode")]
    public bool Verbose { get; set; }


    public static IEnumerable<Option> GetOptions()
    {
        Type thisType = typeof(CommandLineOptions);

        return thisType.GetProperties()
            .Where(p => Attribute.IsDefined(p, typeof(CommandLineOptionAttribute<>)))
            .Select(p => p.GetCustomAttributes(typeof(CommandLineOptionAttribute<>), false).FirstOrDefault() as CommandLineOptionAttribute<object>)
            .Where(o => o != default)
            .Select(o => o!.GetOption());

        // yield return new Option<bool>(GetAliases("--tv", "-t"), "TV tagging mode");
        // yield return new Option<bool>(GetAliases("--movie", "-m"), "Movie tagging mode");

        // yield return new Option<string>(
        //     GetAliases("--config", "-c"),
        //     () => Path.Combine(
        //         Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        //         "autotag",
        //         "conf.json"
        //     ),
        //     "Config file path"
        // );

        // yield return new Option<bool>("--no-rename", "Disable file and subtitle renaming");
        // yield return new Option<bool>("--no-tag", "Disable file tagging");
        // yield return new Option<bool>("--no-cover", "Disable cover art tagging");

        // yield return new Option<bool>("--manual", "Manually choose the TV series for a file from search results");

        // yield return new Option<string?>("--tv-pattern", "Rename pattern for TV episodes");
        // yield return new Option<string?>("--movie-pattern", "Rename pattern for movies");

        // yield return new Option<string?>(GetAliases("--pattern", "-p"), "Custom regex to parse TV episode information");

        // yield return new Option<bool>("--windows-safe", "Remove invalid Windows file name characters when renaming");

        // yield return new Option<bool>("--extended-tagging", "Add more information to Matroska file tags. Reduces tagging speed.");
        // yield return new Option<bool>("--apple-tagging", "Add extra tags to mp4 files for use with Apple devices and software");
        // yield return new Option<bool>("--rename-subs", "Rename subtitle files");

        // yield return new Option<string>(GetAliases("--language", "-l"), () => "en", "Metadata language");

        // yield return new Option<bool>("--set-default", "Set the current arguments as the default");

        // yield return new Option<bool>(GetAliases("--verbose", "-v"), "Enable verbose output mode");

        // yield return new Option<bool>("--version", "Print version number and exit");
    }

    private static string[] GetAliases(string name, string shortName)
        => new[] { name, shortName };
}