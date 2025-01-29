using AutoTag.Core.Config;

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

        if (config.TagFiles && taggingFile.Taggable)
        {
            fileSuccess = await TagFileAsync(taggingFile, metadata);
        }

        if (config.RenameFiles)
        {
            fileSuccess &= RenameFile(taggingFile.Path, metadata.GetFileName(config), null);

            if (!string.IsNullOrEmpty(taggingFile.SubtitlePath))
            {
                fileSuccess &= RenameFile(taggingFile.SubtitlePath, metadata.GetFileName(config), "subtitle ");
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
            if (file != null && file.CorruptionReasons.Any())
            {
                ui.SetStatus($"File corruption reasons: {string.Join(", ", file.CorruptionReasons)})",
                    MessageType.Error | MessageType.Log
                );
            }
            
            fileSuccess = false;
        }

        return fileSuccess;
    }

    private bool RenameFile(string path, string newName, string? msgPrefix)
    {
        bool fileSuccess = true;
        string newPath = Path.Combine(
            Path.GetDirectoryName(path)!,
            GetFileName(
                newName,
                Path.GetFileNameWithoutExtension(path)
            )
            + Path.GetExtension(path)
        );

        if (path != newPath)
        {
            try
            {
                if (fs.Exists(newPath))
                {
                    ui.SetStatus($"Error: Could not rename - {msgPrefix}file already exists", MessageType.Error);
                    fileSuccess = false;
                }
                else
                {
                    fs.Move(path, newPath);
                    ui.SetFilePath(newPath);
                    ui.SetStatus($"Successfully renamed {msgPrefix}file to '{Path.GetFileName(newPath)}'", MessageType.Information);
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