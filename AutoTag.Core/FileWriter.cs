using Microsoft.Extensions.Caching.Memory;

namespace AutoTag.Core;
public class FileWriter : IDisposable
{
    private readonly static HttpClient _client = new HttpClient();
    private static Dictionary<string, byte[]> _images = new Dictionary<string, byte[]>();
    private readonly IMemoryCache _cache;

    public FileWriter()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    public async Task<bool> WriteAsync(TaggingFile taggingFile, FileMetadata metadata, Action<string> setPath, Action<string, MessageType> setStatus, AutoTagConfig config)
    {
        bool fileSuccess = true;

        if (config.TagFiles && taggingFile.Taggable)
        {
            TagLib.File? file = null;
            try
            {
                using (file = TagLib.File.Create(taggingFile.Path))
                {
                    metadata.WriteToFile(file, config, setStatus);

                    // if there is an image available and cover art is enabled
                    if (!string.IsNullOrEmpty(metadata.CoverURL) && config.AddCoverArt == true)
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
                                if (config.Verbose)
                                {
                                    setStatus($"Error: failed to download cover art ({(int) response.StatusCode}:{metadata.CoverURL})", MessageType.Error);
                                }
                                else
                                {
                                    setStatus($"Error: failed to download cover art", MessageType.Error);
                                }
                                fileSuccess = false;
                            }
                        }

                        file.Tag.Pictures = new[] { new TagLib.Picture(imgBytes) { Filename = "cover.jpg" } };
                    }
                    else if (string.IsNullOrEmpty(metadata.CoverURL) && config.AddCoverArt == true)
                    {
                        fileSuccess = false;
                    }

                    file.Save();

                    if (fileSuccess == true)
                    {
                        setStatus($"Successfully tagged file as {metadata}", MessageType.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                if (config.Verbose)
                {
                    if (file != null && file.CorruptionReasons.Any())
                    {
                        setStatus($"Error: Failed to write tags to file ({ex.GetType().Name}: {ex.Message}; CorruptionReasons: {string.Join(", ", file.CorruptionReasons)})", MessageType.Error);
                    }
                    else
                    {
                        setStatus($"Error: Failed to write tags to file ({ex.GetType().Name}: {ex.Message}", MessageType.Error);
                    }
                }
                else
                {
                    setStatus("Error: Failed to write tags to file", MessageType.Error);
                }
                fileSuccess = false;
            }
        }

        if (config.RenameFiles)
        {
            if (invalidFilenameChars == null)
            {
                invalidFilenameChars = Path.GetInvalidFileNameChars();

                if (config.WindowsSafe)
                {
                    invalidFilenameChars = invalidFilenameChars.Union(invalidNtfsChars).ToArray();
                }
            }

            fileSuccess &= RenameFile(taggingFile.Path, metadata.GetFileName(config), setPath, setStatus, config, null);

            if (!string.IsNullOrEmpty(taggingFile.SubtitlePath))
            {
                fileSuccess &= RenameFile(taggingFile.SubtitlePath, metadata.GetFileName(config), setPath, setStatus, config, "subtitle ");
            }
        }

        return fileSuccess;
    }

    private static bool RenameFile(string path, string newName, Action<string> setPath, Action<string, MessageType> setStatus, AutoTagConfig config, string? prefix)
    {
        bool fileSuccess = true;
        string newPath = Path.Combine(
            Path.GetDirectoryName(path)!,
            GetFileName(
                newName,
                Path.GetFileNameWithoutExtension(path),
                setStatus,
                config
            )
            + Path.GetExtension(path)
        );

        if (path != newPath)
        {
            try
            {
                if (File.Exists(newPath))
                {
                    setStatus($"Error: Could not rename - {prefix}file already exists", MessageType.Error);
                    fileSuccess = false;
                }
                else
                {
                    File.Move(path, newPath);
                    setPath(newPath);
                    setStatus($"Successfully renamed {prefix}file to '{Path.GetFileName(newPath)}'", MessageType.Information);
                }
            }
            catch (Exception ex)
            {
                if (config.Verbose)
                {
                    setStatus($"Error: Failed to rename {prefix}file ({ex.GetType().Name}: {ex.Message})", MessageType.Error);
                }
                else
                {
                    setStatus($"Error: Failed to rename {prefix}file", MessageType.Error);
                }
                fileSuccess = false;
            }
        }

        return fileSuccess;
    }

    private static string GetFileName(string fileName, string oldFileName, Action<string, MessageType> setStatus, AutoTagConfig config)
    {
        string result = fileName;
        foreach (var replace in config.FileNameReplaces)
        {
            result = replace.Apply(result);
        }

        string escapedResult = String.Concat(result.Split(invalidFilenameChars));
        if (escapedResult != oldFileName && escapedResult.Length != fileName.Length)
        {
            setStatus("Warning: Invalid characters in file name, automatically removing", MessageType.Warning);
        }

        return escapedResult;
    }

    private static char[]? invalidFilenameChars { get; set; }

    private readonly static char[] invalidNtfsChars = { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };

    private readonly static string[] subtitleFileExtensions = { ".srt", ".vtt", ".sub", ".ssa" };

    public void Dispose()
    {
        _cache.Dispose();
    }
}