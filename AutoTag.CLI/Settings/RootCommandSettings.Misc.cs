namespace AutoTag.CLI.Settings;

public partial class RootCommandSettings
{
    [CommandOption("-c|--config <path>")]
    [Description("Config file path")]
    public required string ConfigPath { get; init; }
    
    [CommandOption("-p|--pattern <pattern>")]
    [Description("Custom regex to parse TV episode information")]
    public string? ParsePattern { get; init; }
    
    [CommandOption("-v|--verbose")]
    [Description("Enable verbose output mode")]
    public bool? Verbose { get; init; }
    
    [CommandOption("--set-default")]
    [Description("Set the current arguments as the default")]
    public bool SetDefault { get; init; }
    
    [CommandOption("--print-config")]
    [Description("Print loaded configuration and exit")]
    public bool PrintConfig { get; init; }
    
    private void SetMiscOptions(AutoTagConfig config)
    {
        if (!string.IsNullOrWhiteSpace(ParsePattern))
        {
            config.ParsePattern = ParsePattern;
        }

        if (Verbose.HasValue)
        {
            config.Verbose = Verbose.Value;
        }
    }
}