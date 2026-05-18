using AutoTag.Core.Files;

namespace AutoTag.Core.Config;

public class AutoTagConfig
{
    public const int CurrentVer = 15;

    public int ConfigVer { get; set; } = CurrentVer;

    public Mode Mode { get; set; } = Mode.Auto;

    public bool ManualMode { get; set; } = false;

    public bool Verbose { get; set; } = false;

    public bool AddCoverArt { get; set; } = true;

    public bool TagFiles { get; set; } = true;

    public bool RenameFiles { get; set; } = true;

    public bool RemoveEmptyFolders { get; set; } = false;

    public string TVRenamePattern { get; set; } =
        "{Series} - {Season}x{Episode:00}{EndEpisode:-00|} - {Title}{Part:pt-0|}";

    public string MovieRenamePattern { get; set; } = "{Title} ({Year})";

    public string? ParsePattern { get; set; }

    public bool WindowsSafe { get; set; } = false;

    public bool ExtendedTagging { get; set; } = false;

    public bool AppleTagging { get; set; } = false;

    public bool RenameSubtitles { get; set; } = false;

    public string Language { get; set; } = "en";

    public List<string> SearchLanguages { get; set; } = [];

    public bool IncludeAdult { get; set; } = false;

    public bool EpisodeGroup { get; set; }

    public IEnumerable<FileNameReplace> FileNameReplaces { get; set; } = [];
}