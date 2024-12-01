namespace AutoTag.CLI.Settings;

public partial class RootCommandSettings
{
    [CommandOption("--no-rename")]
    [Description("Disable file and subtitle renaming")]
    public bool? NoRename { get; init; }

    [CommandOption("--tv-pattern <pattern>")]
    [Description("Rename pattern for TV episodes")]
    public string? TVPattern { get; init; }

    [CommandOption("--movie-pattern <pattern>")]
    [Description("Rename pattern for movies")]
    public string? MoviePattern { get; init; }

    [CommandOption("--windows-safe")]
    [Description("Remove invalid Windows file name characters when renaming")]
    public bool? WindowsSafe { get; init; }

    [CommandOption("--rename-subs")]
    [Description("Rename subtitle files")]
    public bool? RenameSubs { get; init; }

    [CommandOption("--replace <replace=replacement>")]
    [Description("Replace <REPLACE> with <REPLACEMENT> in file names")]
    public IDictionary<string, string>? FileNameReplaces { get; init; }

    private void SetRenameOptions(AutoTagConfig config)
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
            config.FileNameReplaces = FileNameReplace.FromDictionary(FileNameReplaces);
        }
    }
}