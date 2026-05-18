using AutoTag.Core.Config;
using TagLib;
using File = TagLib.File;

namespace AutoTag.Core.Files;

public interface IFileWriter
{
    Task<bool> WriteAsync(TaggingFile taggingFile, FileMetadata metadata);
}

public class FileWriter(
    ICoverArtFetcher coverArtFetcher,
    AutoTagConfig config,
    IFileSystem fs,
    IUserInterface ui,
    IFileNamer namer) : IFileWriter
{
    public async Task<bool> WriteAsync(TaggingFile taggingFile, FileMetadata metadata)
    {
        var fileSuccess = true;

        if (config.TagFiles && taggingFile.Taggable)
        {
            fileSuccess &= await TagFileAsync(taggingFile, metadata);
        }

        if (config.RenameFiles)
        {
            var (targetPath, removedInvalid) = namer.GetNewFileName(metadata);

            var isDirectoryPath = fs.PathContainsDirectory(targetPath);
            var fullTargetPath = GetFullOutputPath(taggingFile.Path, targetPath);

            var subtitlePaths = taggingFile.SubtitlePaths
                .Select((s, i) => (Path: s,
                    NewPath: GetFullOutputPath(s,
                        GetSubtitleTargetFileName(targetPath, i, taggingFile.SubtitlePaths.Count))))
                .ToList();

            if (IsAlreadyNamedCorrectly(taggingFile, fullTargetPath, subtitlePaths))
            {
                ui.SetStatus("Rename skipped - already named correctly", MessageType.Information);
            }
            else
            {
                if (removedInvalid)
                {
                    ui.SetStatus("Warning: Invalid characters in file name, automatically removing",
                        MessageType.Warning);
                }

                var renameSuccess = true;
                renameSuccess &= RenameFile(taggingFile.Path, fullTargetPath, isDirectoryPath, null);

                foreach (var subtitle in subtitlePaths)
                {
                    renameSuccess &= RenameFile(subtitle.Path, subtitle.NewPath, isDirectoryPath, " subtitle");
                }

                if (renameSuccess && isDirectoryPath && config.RemoveEmptyFolders)
                {
                    RemoveSourceDirectoryIfEmpty(fs.GetDirectoryPath(taggingFile.Path),
                        fs.GetDirectoryPath(fullTargetPath)!);
                }

                fileSuccess &= renameSuccess;
            }
        }

        return fileSuccess;
    }

    private async Task<bool> TagFileAsync(TaggingFile taggingFile, FileMetadata metadata)
    {
        var fileSuccess = true;

        File? file = null;
        try
        {
            using (file = File.Create(taggingFile.Path))
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
                        file.Tag.Pictures = [new Picture(imgBytes) { Filename = "cover.jpg" }];
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
            if (file?.CorruptionReasons?.Any() == true)
            {
                ui.SetStatus($"File corruption reasons: {string.Join(", ", file.CorruptionReasons)})",
                    MessageType.Error | MessageType.Log
                );
            }

            fileSuccess = false;
        }
        finally
        {
            file?.Dispose();
        }

        return fileSuccess;
    }

    private bool RenameFile(string path, string newPath, bool isDirectoryPath, string? msgPrefix)
    {
        var fileSuccess = true;
        try
        {
            if (fs.Exists(newPath))
            {
                ui.SetStatus($"Error: Could not rename - {msgPrefix}file already exists", MessageType.Error);
                fileSuccess = false;
            }
            else
            {
                if (isDirectoryPath)
                {
                    fs.CreateDirectory(fs.GetDirectoryPath(newPath)!);
                }

                fs.Move(path, newPath);
                ui.SetFilePath(newPath);

                ui.SetStatus(
                    $"Successfully {(isDirectoryPath ? "moved" : "renamed")} {msgPrefix}file to '{(isDirectoryPath ? newPath : Path.GetFileName(newPath))}'",
                    MessageType.Information);
            }
        }
        catch (Exception ex)
        {
            ui.SetStatus($"Error: Failed to rename {msgPrefix}file", MessageType.Error, ex);
            fileSuccess = false;
        }

        return fileSuccess;
    }

    private void RemoveSourceDirectoryIfEmpty(string? sourceDirectory, string targetDirectory)
    {
        if (string.IsNullOrEmpty(sourceDirectory)
            || sourceDirectory == targetDirectory
            || !fs.DirectoryExists(sourceDirectory)
            || !fs.DirectoryIsEmpty(sourceDirectory))
        {
            return;
        }

        fs.DeleteDirectory(sourceDirectory);
        ui.SetStatus($"Removed empty folder '{sourceDirectory}'", MessageType.Information);
    }

    private string GetFullOutputPath(string path, string newPath)
    {
        var isDirectoryPath = fs.PathContainsDirectory(newPath);
        var extension = Path.GetExtension(path);

        return (isDirectoryPath ? newPath : Path.Combine(fs.GetDirectoryPath(path)!, newPath)) + extension;
    }

    private static bool IsAlreadyNamedCorrectly(TaggingFile taggingFile, string newPath,
        IEnumerable<(string Path, string NewPath)> subtitlePaths)
        => taggingFile.Path == newPath && subtitlePaths.All(p => p.Path == p.NewPath);

    private static string GetSubtitleTargetFileName(string targetFileName, int index, int subtitleCount)
        => subtitleCount == 1
            ? targetFileName
            : $"{targetFileName}.{index + 1}";
}