using AutoTag.Core.Config;

namespace AutoTag.Core.Files;

public interface IFileFinder
{
    List<TaggingFile> FindFilesToProcess(IEnumerable<FileSystemInfo> entries);
}

public class FileFinder(AutoTagConfig config, IFileSystem fs, IUserInterface ui) : IFileFinder
{
    private static readonly string[] VideoExtensions = [".mp4", ".m4v", ".mkv"];
    private static readonly string[] SubtitleExtensions = [".srt", ".vtt", ".sub", ".ssa"];
    
    public List<TaggingFile> FindFilesToProcess(IEnumerable<FileSystemInfo> entries)
    {
        var files = FindFilesInDirectory(entries)
            .DistinctBy(f => f.Path);

        if (config.RenameSubtitles && string.IsNullOrEmpty(config.ParsePattern))
        {
            files = files
                .GroupBy(f => Path.GetFileNameWithoutExtension(f.Path))
                .SelectMany(g => GroupSubtitles(g));
        }

        return files
            .OrderBy(f => f.Path)
            .ToList();
    }
    
    private IEnumerable<TaggingFile> FindFilesInDirectory(IEnumerable<FileSystemInfo> entries)
    {
        foreach (var entry in entries)
        {
            if (entry.Exists)
            {
                if (entry is DirectoryInfo directory)
                {
                    ui.DisplayMessage($"Adding all files in directory '{directory}'", MessageType.Log);
                    
                    foreach (var file in FindFilesInDirectory(fs.GetDirectoryContents(directory)))
                    {
                        yield return file;
                    }
                }
                else if (entry is FileInfo file && IsSupportedFile(file))
                {
                    // add file if not already added and has a supported file extension
                    ui.DisplayMessage($"Adding file '{file}'", MessageType.Log);

                    yield return new TaggingFile
                    {
                        Path = file.FullName,
                        Taggable = VideoExtensions.Contains(file.Extension)
                    };
                }
                else
                {
                    ui.DisplayMessage($"Unsupported file: '{entry}'", MessageType.Log | MessageType.Error);
                }
            }
            else
            {
                ui.DisplayMessage($"Path not found: {entry}", MessageType.Error);
            }
        }
    }
    
    private IEnumerable<TaggingFile> GroupSubtitles(IGrouping<string, TaggingFile> files)
    {
        if (files.Count() == 1)
        {
            yield return files.First();
        }
        else if (files.Count(f => IsVideoFile(Path.GetExtension(f.Path))) > 1
                 || files.Count(f => IsSubtitleFile(Path.GetExtension(f.Path))) > 1)
        {
            ui.DisplayMessage(
                $@"Warning, detected multiple files named ""{files.Key}"", files will be processed separately",
                MessageType.Log | MessageType.Warning
            );
            
            foreach (var f in files)
            {
                yield return f;
            }
        }
        else
        {
            string? videoPath = files.FirstOrDefault(f => IsVideoFile(Path.GetExtension(f.Path)))?.Path;
            string? subPath = files.FirstOrDefault(f => IsSubtitleFile(Path.GetExtension(f.Path)))?.Path;

            if (videoPath != null)
            {
                yield return new TaggingFile
                {
                    Path = videoPath,
                    SubtitlePath = subPath
                };
            }
            else if (subPath != null)
            {
                yield return new TaggingFile
                {
                    Path = subPath,
                    Taggable = false
                };
            }
        }
    }
    
    private bool IsSupportedFile(FileInfo info)
        => IsVideoFile(info.Extension)
           || config.RenameSubtitles && IsSubtitleFile(info.Extension);

    private bool IsVideoFile(string extension) => VideoExtensions.Contains(extension);

    private bool IsSubtitleFile(string extension) => SubtitleExtensions.Contains(extension);
}