using AutoTag.Core.Config;
using AutoTag.Core.Movie;
using AutoTag.Core.TV;

namespace AutoTag.Core.Files;

public interface IFileWriter
{
    Task<bool> WriteAsync(TaggingFile taggingFile, FileMetadata metadata);
}

public class FileWriter(ICoverArtFetcher coverArtFetcher, AutoTagConfig config, IFileSystem fs, IUserInterface ui) : IFileWriter
{
    public async Task<bool> WriteAsync(TaggingFile taggingFile, FileMetadata metadata)
    {
        bool fileSuccess = true;
        var targetFileName = GetFileName(metadata.GetFileName(config), Path.GetFileNameWithoutExtension(taggingFile.Path));
        var targetDirectory = GetTargetDirectory(taggingFile, metadata, targetFileName);

        if (config.RenameFiles && IsAlreadyNamedCorrectly(taggingFile, targetFileName, targetDirectory))
        {
            ui.SetStatus("File skipped - already named correctly", MessageType.Information);
            return true;
        }

        if (config.TagFiles && taggingFile.Taggable)
        {
            fileSuccess = await TagFileAsync(taggingFile, metadata);
        }

        if (config.RenameFiles)
        {
            fileSuccess &= RenameFile(taggingFile.Path, targetFileName, targetDirectory, null);

            var subtitlePaths = GetSubtitlePaths(taggingFile);
            for (var i = 0; i < subtitlePaths.Count; i++)
            {
                fileSuccess &= RenameFile(
                    subtitlePaths[i],
                    GetSubtitleTargetFileName(targetFileName, i, subtitlePaths.Count),
                    targetDirectory,
                    "subtitle "
                );
            }
        }

        return fileSuccess;
    }

    private async Task<bool> TagFileAsync(TaggingFile taggingFile, FileMetadata metadata)
    {
        bool fileSuccess = true;
        
        TagLib.File? file = null;
        try
        {
            using (file = TagLib.File.Create(taggingFile.Path))
            {
                metadata.WriteToFile(file, config, ui);

                // if there is an image available and cover art is enabled
                if (!string.IsNullOrEmpty(metadata.CoverURL) && config.AddCoverArt)
                {
                    var imgBytes = await coverArtFetcher.GetCoverArtAsync(metadata.CoverURL);
                    if (imgBytes == null)
                    {
                        ui.SetStatus(
                            $"Error: failed to download cover art{(config.Verbose ? $"({metadata.CoverURL})" : "")}",
                            MessageType.Error
                        );
                        
                        fileSuccess = false;
                    }
                    else
                    {
                        file.Tag.Pictures = [new TagLib.Picture(imgBytes) { Filename = "cover.jpg" }];
                    }

                }
                else if (string.IsNullOrEmpty(metadata.CoverURL) && config.AddCoverArt)
                {
                    fileSuccess = false;
                }

                file.Save();

                if (fileSuccess)
                {
                    ui.SetStatus($"Successfully tagged file as {metadata}", MessageType.Information);
                }
            }
        }
        catch (Exception ex)
        {
            ui.SetStatus("Error: Failed to write tags to file", MessageType.Error, ex);
            if (file != null && file.CorruptionReasons?.Any() == true)
            {
                ui.SetStatus($"File corruption reasons: {string.Join(", ", file.CorruptionReasons)})",
                    MessageType.Error | MessageType.Log
                );
            }
            
            fileSuccess = false;
        }

        return fileSuccess;
    }

    private bool RenameFile(string path, string newName, string targetDirectory, string? msgPrefix)
    {
        bool fileSuccess = true;
        string newPath = GetTargetPath(path, newName, targetDirectory);

        if (path != newPath)
        {
            var sourceDirectory = Path.GetDirectoryName(path);
            try
            {
                if (fs.Exists(newPath))
                {
                    ui.SetStatus($"Error: Could not rename - {msgPrefix}file already exists", MessageType.Error);
                    fileSuccess = false;
                }
                else
                {
                    fs.CreateDirectory(new DirectoryInfo(targetDirectory));
                    fs.Move(path, newPath);
                    ui.SetFilePath(newPath);
                    ui.SetStatus($"Successfully renamed {msgPrefix}file to '{Path.GetFileName(newPath)}'", MessageType.Information);
                    RemoveSourceDirectoryIfEmpty(sourceDirectory, targetDirectory);
                }
            }
            catch (Exception ex)
            {
                ui.SetStatus($"Error: Failed to rename {msgPrefix}file", MessageType.Error, ex);
                fileSuccess = false;
            }
        }

        return fileSuccess;
    }

    private void RemoveSourceDirectoryIfEmpty(string? sourceDirectory, string targetDirectory)
    {
        if (!config.OrganizeFolders
            || !config.RemoveEmptyFolders
            || string.IsNullOrEmpty(sourceDirectory)
            || Path.GetFullPath(sourceDirectory) == Path.GetFullPath(targetDirectory)
            || !fs.DirectoryExists(sourceDirectory)
            || !fs.DirectoryIsEmpty(sourceDirectory))
        {
            return;
        }

        fs.DeleteDirectory(sourceDirectory);
        ui.SetStatus($"Removed empty folder '{sourceDirectory}'", MessageType.Information);
    }

    private bool IsAlreadyNamedCorrectly(TaggingFile taggingFile, string targetFileName, string targetDirectory)
    {
        if (taggingFile.Path != GetTargetPath(taggingFile.Path, targetFileName, targetDirectory))
        {
            return false;
        }

        var subtitlePaths = GetSubtitlePaths(taggingFile);
        for (var i = 0; i < subtitlePaths.Count; i++)
        {
            if (subtitlePaths[i] != GetTargetPath(
                    subtitlePaths[i],
                    GetSubtitleTargetFileName(targetFileName, i, subtitlePaths.Count),
                    targetDirectory
                ))
            {
                return false;
            }
        }

        return true;
    }

    private string GetTargetPath(string path, string targetFileName, string targetDirectory)
        => Path.Combine(targetDirectory, targetFileName + Path.GetExtension(path));

    private static string GetSubtitleTargetFileName(string targetFileName, int index, int subtitleCount)
        => subtitleCount == 1
            ? targetFileName
            : $"{targetFileName}.{index + 1}";

    private static List<string> GetSubtitlePaths(TaggingFile taggingFile)
    {
        var paths = new List<string>();
        if (!string.IsNullOrEmpty(taggingFile.SubtitlePath))
        {
            paths.Add(taggingFile.SubtitlePath);
        }

        foreach (var subtitlePath in taggingFile.SubtitlePaths)
        {
            if (!string.IsNullOrEmpty(subtitlePath) && !paths.Contains(subtitlePath))
            {
                paths.Add(subtitlePath);
            }
        }

        return paths;
    }

    private string GetTargetDirectory(TaggingFile taggingFile, FileMetadata metadata, string targetFileName)
    {
        var currentDirectory = Path.GetDirectoryName(taggingFile.Path)!;
        if (!config.OrganizeFolders)
        {
            return currentDirectory;
        }

        var rootPath = taggingFile.RootPath ?? currentDirectory;
        return metadata switch
        {
            MovieFileMetadata => Path.Combine(rootPath, targetFileName),
            TVFileMetadata tv => Path.Combine(rootPath, GetFileName(tv.SeriesName, tv.SeriesName), GetSeasonFolderName(tv.Season)),
            _ => currentDirectory
        };
    }

    private static string GetSeasonFolderName(int season)
        => season == 0
            ? "Specials"
            : $"Season {season:00}";

    private string GetFileName(string fileName, string oldFileName)
    {
        string result = fileName;
        foreach (var replace in config.FileNameReplaces)
        {
            result = replace.Apply(result);
        }
        
        var sanitisedName = RemoveInvalidFileNameChars(result);
        if (sanitisedName != oldFileName && sanitisedName.Length != fileName.Length)
        {
            ui.SetStatus("Warning: Invalid characters in file name, automatically removing", MessageType.Warning);
        }

        return sanitisedName;
    }

    private string RemoveInvalidFileNameChars(string fileName)
    {
        if (InvalidFilenameChars == null)
        {
            InvalidFilenameChars = Path.GetInvalidFileNameChars();

            if (config.WindowsSafe)
            {
                InvalidFilenameChars = InvalidFilenameChars.Union(InvalidNtfsChars).ToArray();
            }
        }

        return string.Concat(fileName.Where(c => !InvalidFilenameChars.Contains(c)));
    }

    private static char[]? InvalidFilenameChars { get; set; }
    private static readonly char[] InvalidNtfsChars = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];
}
