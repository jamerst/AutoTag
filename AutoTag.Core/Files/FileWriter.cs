namespace AutoTag.Core.Files;

public interface IFileWriter
{
    Task<bool> WriteAsync(TaggingFile taggingFile, FileMetadata metadata);
}

public class FileWriter(ICoverArtFetcher coverArtFetcher, AutoTagConfig config, IUserInterface ui) : IFileWriter
{
    public async Task<bool> WriteAsync(TaggingFile taggingFile, FileMetadata metadata)
    {
        bool fileSuccess = true;

        if (config.TagFiles && taggingFile.Taggable)
        {
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

                        file.Tag.Pictures = [new TagLib.Picture(imgBytes) { Filename = "cover.jpg" }];
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
                if (config.Verbose && file != null && file.CorruptionReasons.Any())
                {
                    ui.SetStatus($"File corruption reasons: {string.Join(", ", file.CorruptionReasons)})", MessageType.Error);
                }
                
                fileSuccess = false;
            }
        }

        if (config.RenameFiles)
        {
            if (InvalidFilenameChars == null)
            {
                InvalidFilenameChars = Path.GetInvalidFileNameChars();

                if (config.WindowsSafe)
                {
                    InvalidFilenameChars = InvalidFilenameChars.Union(InvalidNtfsChars).ToArray();
                }
            }

            fileSuccess &= RenameFile(taggingFile.Path, metadata.GetFileName(config), null);

            if (!string.IsNullOrEmpty(taggingFile.SubtitlePath))
            {
                fileSuccess &= RenameFile(taggingFile.SubtitlePath, metadata.GetFileName(config), "subtitle ");
            }
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
                if (File.Exists(newPath))
                {
                    ui.SetStatus($"Error: Could not rename - {msgPrefix}file already exists", MessageType.Error);
                    fileSuccess = false;
                }
                else
                {
                    File.Move(path, newPath);
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

        string escapedResult = String.Concat(result.Split(InvalidFilenameChars));
        if (escapedResult != oldFileName && escapedResult.Length != fileName.Length)
        {
            ui.SetStatus("Warning: Invalid characters in file name, automatically removing", MessageType.Warning);
        }

        return escapedResult;
    }

    private static char[]? InvalidFilenameChars { get; set; }
    private static readonly char[] InvalidNtfsChars = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];
}