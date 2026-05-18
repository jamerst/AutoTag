using AutoTag.Core.Config;
using AutoTag.Core.Files.Parsing;

namespace AutoTag.Core.Files;

public interface IFileFinder
{
    List<TaggingFile> FindFilesToProcess(IEnumerable<FileSystemInfo> entries);
}

public class FileFinder(AutoTagConfig config, IFileSystem fs, IUserInterface ui, IFileNameParser parser) : IFileFinder
{
    private static readonly HashSet<string> ProcessableVideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4",
        ".m4v",
        ".mkv",
        ".avi",
        ".mov",
        ".wmv",
        ".mpg",
        ".mpeg",
        ".ts",
        ".m2ts",
        ".mts",
        ".webm",
        ".flv",
        ".3gp",
        ".ogv",
        ".asf",
        ".mxf"
    };

    private static readonly HashSet<string> TaggableVideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4",
        ".m4v",
        ".mkv"
    };

    private static readonly HashSet<string> SubtitleExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".srt",
        ".vtt",
        ".sub",
        ".ssa",
        ".ass"
    };

    public List<TaggingFile> FindFilesToProcess(IEnumerable<FileSystemInfo> entries)
    {
        var files = FindFilesInDirectory(entries)
            .DistinctBy(f => f.Path)
            .Select(f =>
            {
                var (tvResult, movieResult) = parser.ParseFileName(f.Path);

                return f with { TVDetails = tvResult, MovieDetails = movieResult };
            })
            .ToList();

        if (config.RenameSubtitles)
        {
            files = files.GroupBy(f => (f.TVDetails, f.MovieDetails))
                .SelectMany(GroupSubtitles)
                .ToList();
        }

        return files
            .OrderBy(f => f.Path)
            .ToList();
    }

    private IEnumerable<TaggingFile> FindFilesInDirectory(IEnumerable<FileSystemInfo> entries)
    {
        foreach (var entry in entries)
        {
            if (fs.Exists(entry))
            {
                switch (entry)
                {
                    case DirectoryInfo directory:
                        ui.DisplayMessage($"Adding all files in directory '{directory}'", MessageType.Log);

                        foreach (var file in FindFilesInDirectory(fs.GetDirectoryContents(directory)))
                        {
                            yield return file;
                        }

                        break;

                    case FileInfo file when IsSupportedFile(file):
                        // add file if not already added and has a supported file extension
                        ui.DisplayMessage($"Adding file '{file}'", MessageType.Log);

                        yield return new TaggingFile
                        {
                            Path = file.FullName,
                            Taggable = IsTaggableVideoFile(file.Extension)
                        };
                        break;

                    default:
                        ui.DisplayMessage($"Unsupported file: '{entry}'", MessageType.Log | MessageType.Error);
                        break;
                }
            }
            else
            {
                ui.DisplayMessage($"Path not found: {entry}", MessageType.Error);
            }
        }
    }


    private IEnumerable<TaggingFile> GroupSubtitles(
        IGrouping<(ParsedTVFileName? TVResult, ParsedMovieFileName? MovieResult), TaggingFile> files)
    {
        if (files.Key is { TVResult: null, MovieResult: null })
        {
            foreach (var file in files)
            {
                yield return file;
            }
        }
        else if (files.Count() == 1)
        {
            yield return files.First();
        }
        else if (files.Count(f => IsVideoFile(Path.GetExtension(f.Path))) > 1)
        {
            ui.DisplayMessage(
                "Warning, detected possible duplicate video files, files will be processed separately",
                MessageType.Log | MessageType.Warning
            );

            foreach (var f in files)
            {
                yield return f;
            }
        }
        else
        {
            var video = files.FirstOrDefault(f => IsVideoFile(Path.GetExtension(f.Path)));
            var subs = files.Where(f => IsSubtitleFile(Path.GetExtension(f.Path))).ToList();

            if (video != null)
            {
                yield return video with
                {
                    SubtitlePaths = subs.Select(s => s.Path).ToList()
                };
            }
            else if (subs.Count > 0)
            {
                yield return subs[0] with
                {
                    SubtitlePaths = subs.Skip(1).Select(s => s.Path).ToList()
                };
            }
        }
    }


    private bool IsSupportedFile(FileInfo info) =>
        IsVideoFile(info.Extension)
        || (config.RenameSubtitles && IsSubtitleFile(info.Extension));


    private bool IsVideoFile(string extension) => ProcessableVideoExtensions.Contains(extension);

    private bool IsTaggableVideoFile(string extension) => TaggableVideoExtensions.Contains(extension);

    private bool IsSubtitleFile(string extension) => SubtitleExtensions.Contains(extension);
}