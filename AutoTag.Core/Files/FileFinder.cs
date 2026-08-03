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

        if (config.RenameSubtitles || config.RenameExtensions.Count > 0)
        {
            files = files.GroupBy(f => (f.TVDetails, f.MovieDetails))
                .SelectMany(GroupFiles)
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


    private IEnumerable<TaggingFile> GroupFiles(
        IGrouping<(ParsedTVFileName? TVResult, ParsedMovieFileName? MovieResult), TaggingFile> files)
    {
        if (files.Key is { TVResult: null, MovieResult: null })
        {
            foreach (var file in files)
            {
                yield return file;
            }

            yield break;
        }

        if (files.Count() == 1)
        {
            yield return files.First();

            yield break;
        }

        var grouped = files.Aggregate(
            new
            {
                Videos = new List<TaggingFile>(),
                Subtitles = new List<TaggingFile>(),
                Other = new List<TaggingFile>()
            },
            (grouped, file) =>
            {
                var extension = Path.GetExtension(file.Path);
                if (IsVideoFile(extension))
                {
                    grouped.Videos.Add(file);
                }
                else if (IsSubtitleFile(extension))
                {
                    grouped.Subtitles.Add(file);
                }
                else
                {
                    grouped.Other.Add(file);
                }

                return grouped;
            }
        );

        if (grouped.Videos.Count > 1)
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
            var video = grouped.Videos.FirstOrDefault();
            var additionalFiles = grouped.Subtitles.Select(s => new AdditionalFile(s.Path, true))
                .Union(grouped.Other.Select(o => new AdditionalFile(o.Path, false)))
                .ToList();

            if (video != null)
            {
                yield return video with
                {
                    AdditionalPaths = additionalFiles
                };
            }
            else
            {
                var first = files.First(f => f.Path == additionalFiles[0].Path);

                yield return first with
                {
                    AdditionalPaths = additionalFiles.Skip(1).ToList()
                };
            }
        }
    }


    private bool IsSupportedFile(FileInfo info) =>
        IsVideoFile(info.Extension)
        || IsSubtitleFile(info.Extension)
        || IsAdditionalFile(info.Extension);


    private static bool IsVideoFile(string extension) => ProcessableVideoExtensions.Contains(extension);

    private static bool IsTaggableVideoFile(string extension) => TaggableVideoExtensions.Contains(extension);

    private bool IsSubtitleFile(string extension) =>
        config.RenameSubtitles && SubtitleExtensions.Contains(extension);

    private bool IsAdditionalFile(string extension) => config.RenameExtensions.Contains(extension.ToLower());
}