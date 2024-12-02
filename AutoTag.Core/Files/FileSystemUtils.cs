namespace AutoTag.Core.Files;

public static class FileSystemUtils
{
    public static IEnumerable<TaggingFile> FindFiles(IEnumerable<FileSystemInfo> entries, IEnumerable<string> supportedExtensions, IUserInterface ui)
    {
        foreach (var entry in entries)
        {
            if (entry.Exists)
            {
                if (entry is DirectoryInfo directory)
                {
                    ui.DisplayMessage($"Adding all files in directory '{directory}'", MessageType.Log);
                    
                    foreach (var file in FindFiles(directory.GetFileSystemInfos(), supportedExtensions, ui))
                    {
                        yield return file;
                    }
                }
                else if (entry is FileInfo file && supportedExtensions.Contains(file.Extension))
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

    public static IEnumerable<TaggingFile> FindSubtitles(IGrouping<string, TaggingFile> files, IUserInterface ui)
    {
        if (files.Count() == 1)
        {
            yield return files.First();
        }
        else if (files.Count(f => VideoExtensions.Contains(Path.GetExtension(f.Path))) > 1
            || files.Count(f => SubtitleExtensions.Contains(Path.GetExtension(f.Path))) > 1)
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
            string? videoPath = files.FirstOrDefault(f => VideoExtensions.Contains(Path.GetExtension(f.Path)))?.Path;
            string? subPath = files.FirstOrDefault(f => SubtitleExtensions.Contains(Path.GetExtension(f.Path)))?.Path;

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

    public static readonly string[] VideoExtensions = [".mp4", ".m4v", ".mkv"];
    public static readonly string[] SubtitleExtensions = [".srt", ".vtt", ".sub", ".ssa"];
}