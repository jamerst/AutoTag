using AutoTag.Core.Config;
using AutoTag.Core.TV;
using System.Text.RegularExpressions;

namespace AutoTag.Core.Files;

public interface IFileFinder
{
    List<TaggingFile> FindFilesToProcess(IEnumerable<FileSystemInfo> entries);
}

public class FileFinder(AutoTagConfig config, IFileSystem fs, IUserInterface ui) : IFileFinder
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
            .ToList();

        if (config.RenameSubtitles && string.IsNullOrEmpty(config.ParsePattern))
        {
            files = files
                .GroupBy(f => Path.GetFileNameWithoutExtension(f.Path))
                .SelectMany(g => GroupSubtitles(g))
                .ToList();

            files = AttachLooseSubtitles(files).ToList();
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
                    
                    foreach (var file in FindFilesInDirectory(fs.GetDirectoryContents(directory), directory.FullName))
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
                        RootPath = file.DirectoryName,
                        Taggable = IsTaggableVideoFile(file.Extension)
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

    private IEnumerable<TaggingFile> FindFilesInDirectory(IEnumerable<FileSystemInfo> entries, string rootPath)
    {
        foreach (var file in FindFilesInDirectory(entries))
        {
            file.RootPath = rootPath;
            yield return file;
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
                    RootPath = files.FirstOrDefault(f => f.Path == videoPath)?.RootPath,
                    SubtitlePath = subPath,
                    SubtitlePaths = subPath != null ? [subPath] : [],
                    Taggable = IsTaggableVideoFile(Path.GetExtension(videoPath))
                };
            }
            else if (subPath != null)
            {
                yield return new TaggingFile
                {
                    Path = subPath,
                    RootPath = files.FirstOrDefault(f => f.Path == subPath)?.RootPath,
                    Taggable = false
                };
            }
        }
    }

    private IEnumerable<TaggingFile> AttachLooseSubtitles(List<TaggingFile> files)
    {
        var handledSubtitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var videosByEpisode = files
            .Where(IsVideoFile)
            .Select(file => new { File = file, Key = GetVideoEpisodeKey(file) })
            .Where(item => item.Key.HasValue)
            .GroupBy(item => item.Key!.Value)
            .ToDictionary(group => group.Key, group => group.Select(item => item.File).ToList());

        foreach (var subtitle in files.Where(IsSubtitleFile))
        {
            foreach (var key in GetSubtitleEpisodeKeys(subtitle))
            {
                if (!videosByEpisode.TryGetValue(key, out var videos) || videos.Count != 1)
                {
                    continue;
                }

                AddSubtitlePath(videos[0], subtitle.Path);
                handledSubtitles.Add(subtitle.Path);
                break;
            }
        }

        return files.Where(file => !handledSubtitles.Contains(file.Path));
    }
    
    private EpisodeKey? GetVideoEpisodeKey(TaggingFile file)
    {
        if (!EpisodeParser.TryParseEpisodeInfo(Path.GetFileName(file.Path), out var metadata, out _))
        {
            return null;
        }

        return new EpisodeKey(NormaliseSeriesName(metadata.SeriesName), metadata.Season, metadata.Episode);
    }

    private IEnumerable<EpisodeKey> GetSubtitleEpisodeKeys(TaggingFile file)
    {
        if (EpisodeParser.TryParseEpisodeInfo(Path.GetFileName(file.Path), out var metadata, out _))
        {
            yield return new EpisodeKey(NormaliseSeriesName(metadata.SeriesName), metadata.Season, metadata.Episode);
            yield break;
        }

        var cleanedName = BracketGroupRegex.Replace(Path.GetFileNameWithoutExtension(file.Path), " ");
        var matches = LooseEpisodeNumberRegex.Matches(cleanedName)
            .Where(match => int.TryParse(match.Groups["Episode"].Value, out var episode) && episode != 720)
            .ToList();
        var episodeMatch = matches.LastOrDefault();
        if (episodeMatch == null || !int.TryParse(episodeMatch.Groups["Episode"].Value, out var episodeNumber))
        {
            yield break;
        }

        var season = GetSeasonFromPath(file.Path);
        var seriesNames = new List<string>();
        var parsedSeriesName = cleanedName[..episodeMatch.Groups["Episode"].Index].Trim(' ', '.', '-', '_');
        if (!string.IsNullOrWhiteSpace(parsedSeriesName))
        {
            seriesNames.Add(parsedSeriesName);
        }

        var directorySeriesName = GetSeriesNameFromPath(file);
        if (!string.IsNullOrWhiteSpace(directorySeriesName))
        {
            seriesNames.Add(directorySeriesName);
        }

        foreach (var seriesName in seriesNames
                     .Select(NormaliseSeriesName)
                     .Where(name => !string.IsNullOrWhiteSpace(name))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            yield return new EpisodeKey(seriesName, season, episodeNumber);
        }
    }

    private static void AddSubtitlePath(TaggingFile video, string subtitlePath)
    {
        if (!string.IsNullOrEmpty(video.SubtitlePath) && !video.SubtitlePaths.Contains(video.SubtitlePath))
        {
            video.SubtitlePaths.Add(video.SubtitlePath);
        }

        if (!video.SubtitlePaths.Contains(subtitlePath))
        {
            video.SubtitlePaths.Add(subtitlePath);
        }

        video.SubtitlePath ??= subtitlePath;
    }
    
    private static string? GetSeriesNameFromPath(TaggingFile file)
    {
        if (string.IsNullOrEmpty(file.RootPath))
        {
            return null;
        }

        var relativePath = Path.GetRelativePath(file.RootPath, file.Path);
        return relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).FirstOrDefault();
    }

    private static int GetSeasonFromPath(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (directory == null)
        {
            return 1;
        }

        foreach (var segment in directory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Reverse())
        {
            if (segment.Equals("Specials", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            var match = SeasonFolderRegex.Match(segment);
            if (match.Success && int.TryParse(match.Groups["Season"].Value, out var season))
            {
                return season;
            }
        }

        return 1;
    }
    
    private static string NormaliseSeriesName(string seriesName)
        => MultiSpaceRegex.Replace(seriesName.Replace('.', ' ').Replace('_', ' '), " ")
            .Trim(' ', '.', '-', '_')
            .ToUpperInvariant();
    
    private bool IsSupportedFile(FileInfo info)
        => IsVideoFile(info.Extension)
           || config.RenameSubtitles && IsSubtitleFile(info.Extension);

    private bool IsVideoFile(TaggingFile file) => IsVideoFile(Path.GetExtension(file.Path));

    private bool IsVideoFile(string extension) => ProcessableVideoExtensions.Contains(extension);

    private bool IsSubtitleFile(TaggingFile file) => IsSubtitleFile(Path.GetExtension(file.Path));

    private bool IsTaggableVideoFile(string extension) => TaggableVideoExtensions.Contains(extension);

    private bool IsSubtitleFile(string extension) => SubtitleExtensions.Contains(extension);

    private readonly record struct EpisodeKey(string SeriesName, int Season, int Episode);

    private static readonly Regex BracketGroupRegex = new(@"\[[^\]]+\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex LooseEpisodeNumberRegex = new(@"(?:^|[._\s-])(?<Episode>\d{1,3})(?=$|[._\s-])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex MultiSpaceRegex = new(@"\s+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex SeasonFolderRegex = new(@"^Season\s*(?<Season>\d+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
}
