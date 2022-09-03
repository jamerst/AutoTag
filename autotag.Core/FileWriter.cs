using Microsoft.Extensions.Caching.Memory;

namespace autotag.Core;
public class FileWriter : IDisposable
{
    private readonly static HttpClient _client = new HttpClient();
    private static Dictionary<string, byte[]> _images = new Dictionary<string, byte[]>();
    private readonly IMemoryCache _cache;

    public FileWriter()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    public async Task<bool> WriteAsync(string filePath, FileMetadata metadata, Action<string> setPath, Action<string, MessageType> setStatus, AutoTagConfig config)
    {
        bool fileSuccess = true;
        if (config.TagFiles)
        {
            if (invalidFilenameChars == null)
            {
                invalidFilenameChars = Path.GetInvalidFileNameChars();

                if (config.WindowsSafe)
                {
                    invalidFilenameChars = invalidFilenameChars.Union(invalidNtfsChars).ToArray();
                }
            }

            TagLib.File? file = null;
            try
            {
                using (file = TagLib.File.Create(filePath))
                {
                    metadata.WriteToFile(file, config, setStatus);

                    // if there is an image available and cover art is enabled
                    if (!string.IsNullOrEmpty(metadata.CoverFilename) && config.AddCoverArt == true)
                    {
                        byte[] imgBytes;
                        if (!_cache.TryGetValue<byte[]>(metadata.CoverURL, out imgBytes))
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
                    else if (string.IsNullOrEmpty(metadata.CoverFilename) && config.AddCoverArt == true)
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
            string newPath = Path.Combine(
                Path.GetDirectoryName(filePath)!,
                EscapeFilename(
                    metadata.GetFileName(config),
                    Path.GetFileNameWithoutExtension(filePath),
                    setStatus
                )
                + Path.GetExtension(filePath)
            );

            if (filePath != newPath)
            {
                try
                {
                    if (File.Exists(newPath))
                    {
                        setStatus("Error: Could not rename - file already exists", MessageType.Error);
                        fileSuccess = false;
                    }
                    else
                    {
                        File.Move(filePath, newPath);
                        setPath(newPath);
                        setStatus($"Successfully renamed file to '{Path.GetFileName(newPath)}'", MessageType.Information);
                    }
                }
                catch (Exception ex)
                {
                    if (config.Verbose)
                    {
                        setStatus($"Error: Failed to rename file ({ex.GetType().Name}: {ex.Message})", MessageType.Error);
                    }
                    else
                    {
                        setStatus("Error: Failed to rename file", MessageType.Error);
                    }
                    fileSuccess = false;
                }
            }
        }

        return fileSuccess;
    }

    private static string EscapeFilename(string filename, string oldFilename, Action<string, MessageType> setStatus)
    {
        string result = String.Join("", filename.Split(invalidFilenameChars));
        if (result != oldFilename && result.Length != filename.Length)
        {
            setStatus("Warning: Invalid characters in file name, automatically removing", MessageType.Warning);
        }

        return result;
    }

    private static char[]? invalidFilenameChars { get; set; }

    private readonly static char[] invalidNtfsChars = { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };

    public void Dispose()
    {
        _cache.Dispose();
    }
}