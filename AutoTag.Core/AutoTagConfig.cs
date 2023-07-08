namespace AutoTag.Core;
public class AutoTagConfig
{
    public enum Modes { TV, Movie };
    public const int CurrentVer = 10;
    public int ConfigVer { get; set; } = CurrentVer;
    public Modes Mode { get; set; } = Modes.TV;
    public bool ManualMode { get; set; } = false;
    public bool Verbose { get; set; } = false;
    public bool AddCoverArt { get; set; } = true;
    public bool TagFiles { get; set; } = true;
    public bool RenameFiles { get; set; } = true;
    public string TVRenamePattern { get; set; } = "%1 - %2x%3:00 - %4";
    public string MovieRenamePattern { get; set; } = "%1 (%2)";
    public string? ParsePattern { get; set; }
    public bool WindowsSafe { get; set; } = false;
    public bool ExtendedTagging { get; set; } = false;
    public bool AppleTagging { get; set; } = false;
    public bool RenameSubtitles { get; set; } = false;
    public string Language { get; set; } = "en";
    public bool EpisodeGroup { get; set; }
    public IEnumerable<FileNameReplace> FileNameReplaces { get; set; } = Enumerable.Empty<FileNameReplace>();

    public bool IsTVMode() => Mode == Modes.TV;
}