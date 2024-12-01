namespace AutoTag.CLI.Settings;

public partial class RootCommandSettings
{
    [CommandOption("-t|--tv")]
    [Description("TV tagging mode")]
    public bool TVMode { get; init; }
    
    [CommandOption("-m|--movie")]
    [Description("Movie tagging mode")]
    public bool MovieMode { get; init; }
    
    [CommandOption("--no-tag")]
    [Description("Disable file tagging")]
    public bool? NoTag { get; init; }
    
    [CommandOption("--no-cover")]
    [Description("Disable cover art tagging")]
    public bool? NoCover { get; init; }
    
    [CommandOption("--manual")]
    [Description("Manually choose the TV series/movie for a file from search results")]
    public bool? Manual { get; init; }
    
    [CommandOption("--extended-tagging")]
    [Description("Add more information to Matroska file tags. Reduces tagging speed.")]
    public bool? ExtendedTagging { get; init; }
    
    [CommandOption("--apple-tagging")]
    [Description("Add extra tags to mp4 files for use with Apple devices and software")]
    public bool? AppleTagging { get; init; }
    
    [CommandOption("-l|--language <language>")]
    [Description("Metadata language")]
    public string? Language { get; init; }
    
    [CommandOption("-g|--episode-group")]
    [Description("Manually choose the Episode Group for a TV episode. Also enables manual mode.")]
    public bool? EpisodeGroup { get; init; }

    private void SetTaggingOptions(AutoTagConfig config)
    {
        if (TVMode)
        {
            config.Mode = AutoTagConfig.Modes.TV;
        }

        if (MovieMode)
        {
            config.Mode = AutoTagConfig.Modes.Movie;
        }

        if (NoTag.HasValue)
        {
            config.TagFiles = !NoTag.Value;
        }

        if (NoCover.HasValue)
        {
            config.AddCoverArt = !NoCover.Value;
        }

        if (Manual.HasValue)
        {
            config.ManualMode = Manual.Value;
        }

        if (ExtendedTagging.HasValue)
        {
            config.ExtendedTagging = ExtendedTagging.Value;
        }

        if (AppleTagging.HasValue)
        {
            config.AppleTagging = AppleTagging.Value;
        }

        if (!string.IsNullOrWhiteSpace(Language))
        {
            config.Language = Language;
        }

        if (EpisodeGroup.HasValue)
        {
            config.EpisodeGroup = EpisodeGroup.Value;
        }
    }
}