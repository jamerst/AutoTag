using AutoTag.Core.Config;
using AutoTag.Core.Tagging;

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
    IFileNamer namer,
    ITagger tagger) : IFileWriter
{
    public async Task<bool> WriteAsync(TaggingFile taggingFile, FileMetadata metadata)
    {
        var fileSuccess = true;

        if (config.TagFiles && taggingFile.Taggable)
        {
            fileSuccess &= await tagger.TagFileAsync(taggingFile, metadata);
        }

        if (config.RenameFiles)
        {
            var (targetPath, removedInvalid) = namer.GetNewFileName(metadata);

            var isDirectoryPath = fs.PathContainsDirectory(targetPath);
            var fullTargetPath = GetFullOutputPath(taggingFile.Path, targetPath);

            var additionalFiles = taggingFile.AdditionalPaths
                .GroupBy(f => Path.GetExtension(f.Path).ToLower())
                .SelectMany(g => g.Select((f, i) =>
                    (
                        f.Path,
                        f.Subtitle,
                        NewPath: GetFullOutputPath(f.Path, g.Count() == 1 ? targetPath : $"{targetPath}.{i + 1}")
                    )
                ))
                .ToList();

            if (IsAlreadyNamedCorrectly(taggingFile, fullTargetPath, additionalFiles))
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

                foreach (var file in additionalFiles)
                {
                    renameSuccess &= RenameFile(file.Path, file.NewPath, isDirectoryPath,
                        file.Subtitle ? " subtitle" : null);
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
        IEnumerable<(string Path, bool Subtitle, string NewPath)> additionalPaths)
        => taggingFile.Path == Path.GetFullPath(newPath) &&
            additionalPaths.All(p => p.Path == Path.GetFullPath(p.NewPath));
}