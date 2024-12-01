using Microsoft.Extensions.Caching.Memory;

namespace AutoTag.Core;
public class FileWriter : IDisposable
{
    private static readonly HttpClient _client = new();
    private readonly IMemoryCache _cache;
    private readonly AutoTagConfig _config;

    public FileWriter(AutoTagConfig config)
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _config = config;
    }

    public async Task<bool> WriteAsync(TaggingFile taggingFile, FileMetadata metadata, IUserInterface ui)
    {
        bool fileSuccess = true;

        if (_config.TagFiles && taggingFile.Taggable)
        {
            TagLib.File? file = null;
            try
            {
                using (file = TagLib.File.Create(taggingFile.Path))
                {
                    metadata.WriteToFile(file, _config, ui);

                    // if there is an image available and cover art is enabled
                    if (!string.IsNullOrEmpty(metadata.CoverURL) && _config.AddCoverArt == true)
                    {
                        byte[] imgBytes;
                        if (!_cache.TryGetValue(metadata.CoverURL, out imgBytes!))
                        {
                            var response = await _client.GetAsync(metadata.CoverURL, HttpCompletionOption.ResponseHeadersRead);
                            if (response.IsSuccessStatusCode)
                            {
                                imgBytes = await response.Content.ReadAsByteArrayAsync();
                                _cache.Set(metadata.CoverURL, imgBytes);
                            }
                            else
                            {
                                if (_config.Verbose)
                                {
                                    ui.SetStatus($"Error: failed to download cover art ({(int) response.StatusCode}:{metadata.CoverURL})", MessageType.Error);
                                }
                                else
                                {
                                    ui.SetStatus($"Error: failed to download cover art", MessageType.Error);
                                }
                                fileSuccess = false;
                            }
                        }

                        file.Tag.Pictures = [new TagLib.Picture(imgBytes) { Filename = "cover.jpg" }];
                    }
                    else if (string.IsNullOrEmpty(metadata.CoverURL) && _config.AddCoverArt)
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
                if (_config.Verbose)
                {
                    if (file != null && file.CorruptionReasons.Any())
                    {
                        ui.SetStatus($"Error: Failed to write tags to file ({ex.GetType().Name}: {ex.Message}; CorruptionReasons: {string.Join(", ", file.CorruptionReasons)})", MessageType.Error);
                    }
                    else
                    {
                        ui.SetStatus($"Error: Failed to write tags to file ({ex.GetType().Name}: {ex.Message}", MessageType.Error);
                    }
                }
                else
                {
                    ui.SetStatus("Error: Failed to write tags to file", MessageType.Error);
                }
                fileSuccess = false;
            }
        }

        if (_config.RenameFiles)
        {
            if (invalidFilenameChars == null)
            {
                invalidFilenameChars = Path.GetInvalidFileNameChars();

                if (_config.WindowsSafe)
                {
                    invalidFilenameChars = invalidFilenameChars.Union(invalidNtfsChars).ToArray();
                }
            }

            fileSuccess &= RenameFile(taggingFile.Path, metadata.GetFileName(_config), null, ui);

            if (!string.IsNullOrEmpty(taggingFile.SubtitlePath))
            {
                fileSuccess &= RenameFile(taggingFile.SubtitlePath, metadata.GetFileName(_config), "subtitle ", ui);
            }
        }

        return fileSuccess;
    }

    private bool RenameFile(string path, string newName, string? msgPrefix, IUserInterface ui)
    {
        bool fileSuccess = true;
        string newPath = Path.Combine(
            Path.GetDirectoryName(path)!,
            GetFileName(
                newName,
                Path.GetFileNameWithoutExtension(path),
                ui
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
                if (_config.Verbose)
                {
                    ui.SetStatus($"Error: Failed to rename {msgPrefix}file ({ex.GetType().Name}: {ex.Message})", MessageType.Error);
                }
                else
                {
                    ui.SetStatus($"Error: Failed to rename {msgPrefix}file", MessageType.Error);
                }
                fileSuccess = false;
            }
        }

        return fileSuccess;
    }

    private string GetFileName(string fileName, string oldFileName, IUserInterface ui)
    {
        string result = fileName;
        foreach (var replace in _config.FileNameReplaces)
        {
            result = replace.Apply(result);
        }

        string escapedResult = String.Concat(result.Split(invalidFilenameChars));
        if (escapedResult != oldFileName && escapedResult.Length != fileName.Length)
        {
            ui.SetStatus("Warning: Invalid characters in file name, automatically removing", MessageType.Warning);
        }

        return escapedResult;
    }

    private static char[]? invalidFilenameChars { get; set; }
    private static readonly char[] invalidNtfsChars = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];

    public void Dispose()
    {
        _cache.Dispose();
    }
}