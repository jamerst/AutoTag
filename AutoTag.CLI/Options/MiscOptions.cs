namespace AutoTag.CLI.Options;

public class MiscOptions : OptionsBase<MiscOptions>, IOptionsBase<MiscOptions>
{
    [CommandLineOption<string>("--config", "-c", "Config file path")]
    public string ConfigPath { get; set; } = null!;

    [CommandLineOption<string>("--pattern", "-p", "Custom regex to parse TV episode information")]
    public string? ParsePattern { get; set; }

    [CommandLineOption<bool?>("--verbose", "-v", "Enable verbose output mode")]
    public bool? Verbose { get; set; }

    [CommandLineOption<bool>("--set-default", "Set the current arguments as the default")]
    public bool SetDefault { get; set; }

    [CommandLineOption<bool>("--print-config", "Print loaded configuration and exit")]
    public bool PrintConfig { get; set; }

    public static IEnumerable<Option> GetOptions()
    {
        yield return GetOption(o => o.ConfigPath, GetDefaultConfigPath());
        yield return GetOption(o => o.ParsePattern);
        yield return GetOption(o => o.Verbose);
        yield return GetOption(o => o.SetDefault);
        yield return GetOption(o => o.PrintConfig);
    }

    public static MiscOptions GetBoundValues(BindingContext context) =>
        new MiscOptions
        {
            ConfigPath = GetValueForProperty(o => o.ConfigPath, context),
            ParsePattern = GetValueForProperty(o => o.ParsePattern, context),
            Verbose = GetValueForProperty(o => o.Verbose, context),
            SetDefault = GetValueForProperty(o => o.SetDefault, context),
            PrintConfig = GetValueForProperty(o => o.PrintConfig, context)
        };

    public void UpdateConfig(AutoTagConfig config)
    {
        config.ParsePattern = string.IsNullOrWhiteSpace(ParsePattern) ? config.ParsePattern : ParsePattern;
        config.Verbose = Verbose ?? config.Verbose;
    }

    private static string GetDefaultConfigPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "autotag",
        "conf.json"
    );
}