using AutoTag.Core.Config;

namespace AutoTag.Core.Files;
public class TaggingFile
{
    public string Path { get; set; } = null!;
    public string? SubtitlePath { get; set; }
    public bool Taggable { get; set; } = true;
    public string Status { get; set; } = "";
    public bool Success { get; set; } = true;

    public override string ToString()
    {
        return $"{System.IO.Path.GetFileName(Path)}: {Status}";
    }

    public static List<TaggingFile> FindTaggingFiles(IEnumerable<FileSystemInfo> paths, AutoTagConfig config, IUserInterface ui)
    {
        IEnumerable<string> supportedExtensions = FileSystemUtils.VideoExtensions;
        if (config.RenameSubtitles)
        {
            supportedExtensions = supportedExtensions.Concat(FileSystemUtils.SubtitleExtensions);
        }

        var tempFiles = FileSystemUtils.FindFiles(paths, supportedExtensions, ui)
            .DistinctBy(f => f.Path);

        if (config.RenameSubtitles && string.IsNullOrEmpty(config.ParsePattern))
        {
            tempFiles = tempFiles
                .GroupBy(f => System.IO.Path.GetFileNameWithoutExtension(f.Path))
                .SelectMany(f => FileSystemUtils.FindSubtitles(f, ui));
        }

        return tempFiles
            .OrderBy(f => f.Path)
            .ToList();
    }
}