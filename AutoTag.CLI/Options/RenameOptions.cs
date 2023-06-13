namespace AutoTag.CLI.Options;

public class RenameOptions : OptionsBase<RenameOptions>, IOptionsBase<RenameOptions>
{
    [CommandLineOption<bool?>("--no-rename", "Disable file and subtitle renaming")]
    public bool? NoRename { get; set; }

    [CommandLineOption<string>("--tv-pattern", "Rename pattern for TV episodes")]
    public string? TVPattern { get; set; }
    [CommandLineOption<string>("--movie-pattern", "Rename pattern for movies")]
    public string? MoviePattern { get; set; }

    [CommandLineOption<bool?>("--windows-safe", "Remove invalid Windows file name characters when renaming")]
    public bool? WindowsSafe { get; set; }

    [CommandLineOption<bool?>("--rename-subs", "Rename subtitle files")]
    public bool? RenameSubs { get; set; }

    [CommandLineOption<IEnumerable<string>>("--replace", "Replace <replace> with <replacement> in file names")]
    public IEnumerable<string>? FileNameReplaces { get; set; }

    public static IEnumerable<Option> GetOptions()
    {
        yield return GetOption(o => o.NoRename);
        yield return GetOption(o => o.TVPattern);
        yield return GetOption(o => o.MoviePattern);
        yield return GetOption(o => o.WindowsSafe);
        yield return GetOption(o => o.RenameSubs);
        yield return GetOption(
            o => o.FileNameReplaces,
            o =>
            {
                o.AllowMultipleArgumentsPerToken = true;
                o.Arity = new ArgumentArity(2, 2);
                o.ArgumentHelpName = "replace replacement";
            }
        );
    }

    public static RenameOptions GetBoundValues(BindingContext context) =>
        new RenameOptions
        {
            NoRename = GetValueForProperty(o => o.NoRename, context),
            TVPattern = GetValueForProperty(o => o.TVPattern, context),
            MoviePattern = GetValueForProperty(o => o.MoviePattern, context),
            WindowsSafe = GetValueForProperty(o => o.WindowsSafe, context),
            RenameSubs = GetValueForProperty(o => o.RenameSubs, context),
            FileNameReplaces = GetValueForProperty(o => o.FileNameReplaces, context),
        };

    public void UpdateConfig(AutoTagConfig config)
    {
        if (NoRename.HasValue)
        {
            config.RenameFiles = !NoRename.Value;
        }

        if (!string.IsNullOrEmpty(TVPattern))
        {
            config.TVRenamePattern = TVPattern;
        }

        if (!string.IsNullOrEmpty(MoviePattern))
        {
            config.MovieRenamePattern = MoviePattern;
        }

        if (WindowsSafe.HasValue)
        {
            config.WindowsSafe = WindowsSafe.Value;
        }

        if (RenameSubs.HasValue)
        {
            config.RenameSubtitles = RenameSubs.Value;
        }

        if (FileNameReplaces != null && FileNameReplaces.Any())
        {
            config.FileNameReplaces = FileNameReplace.FromStrings(FileNameReplaces);
        }
    }
}