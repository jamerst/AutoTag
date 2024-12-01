namespace AutoTag.CLI.Options;

public class TaggingOptions : OptionsBase<TaggingOptions>, IOptionsBase<TaggingOptions>
{
    [CommandLineOption<bool>("--tv", "-t", "TV tagging mode")]
    public bool TVMode { get; set; }

    [CommandLineOption<bool>("--movie", "-m", "Movie tagging mode")]
    public bool MovieMode { get; set; }

    [CommandLineOption<bool?>("--no-tag", "Disable file tagging")]
    public bool? NoTag { get; set; }
    [CommandLineOption<bool?>("--no-cover", "Disable cover art tagging")]
    public bool? NoCover { get; set; }

    [CommandLineOption<bool?>("--manual", "Manually choose the TV series for a file from search results")]
    public bool? Manual { get; set; }

    [CommandLineOption<bool?>("--extended-tagging", "Add more information to Matroska file tags. Reduces tagging speed.")]
    public bool? ExtendedTagging { get; set; }
    [CommandLineOption<bool?>("--apple-tagging", "Add extra tags to mp4 files for use with Apple devices and software")]
    public bool? AppleTagging { get; set; }

    [CommandLineOption<string>("--language", "-l", "Metadata language")]
    public string? Language { get; set; }
    
    [CommandLineOption<bool?>("--episode-group", "-g", "Manually choose the Episode Group for a TV episode. Enables manual mode.")] 
    public bool? EpisodeGroup { get; set; }

    public static IEnumerable<Option> GetOptions()
    {
        yield return GetOption(o => o.TVMode);
        yield return GetOption(o => o.MovieMode);
        yield return GetOption(o => o.NoTag);
        yield return GetOption(o => o.NoCover);
        yield return GetOption(o => o.Manual);
        yield return GetOption(o => o.ExtendedTagging);
        yield return GetOption(o => o.AppleTagging);
        yield return GetOption(o => o.Language);
        yield return GetOption(o => o.EpisodeGroup);
    }

    public static TaggingOptions GetBoundValues(BindingContext context) =>
        new TaggingOptions
        {
            TVMode = GetValueForProperty(o => o.TVMode, context),
            MovieMode = GetValueForProperty(o => o.MovieMode, context),
            NoTag = GetValueForProperty(o => o.NoTag, context),
            NoCover = GetValueForProperty(o => o.NoCover, context),
            Manual = GetValueForProperty(o => o.Manual, context),
            ExtendedTagging = GetValueForProperty(o => o.ExtendedTagging, context),
            AppleTagging = GetValueForProperty(o => o.AppleTagging, context),
            Language = GetValueForProperty(o => o.Language, context),
            EpisodeGroup = GetValueForProperty(o => o.EpisodeGroup, context),
        };

    public void UpdateConfig(AutoTagConfig config)
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