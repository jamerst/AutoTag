using AutoTag.Core.Config;
using AutoTag.Core.Files;
using TagLibSharp2.Core;
using TagLibSharp2.Matroska;
using TagLibSharp2.Mp4;
using AutoTagFileSystem = AutoTag.Core.Files.IFileSystem;

namespace AutoTag.Core.Tagging;

public class TagLib2Tagger(
    AutoTagFileSystem fs,
    IUserInterface ui,
    ICoverArtFetcher coverArtFetcher,
    AutoTagConfig config) : ITagger
{
    public async Task<bool> TagFileAsync(TaggingFile taggingFile, FileMetadata metadata)
    {
        var fileSuccess = true;

        var data = await fs.ReadAllBytesAsync(taggingFile.Path);

        if (!TryReadFile(taggingFile, data, out var file))
        {
            return false;
        }

        using (file)
        {
            var fileTags = file.Tag!;

            var tags = metadata.GetTags(config);

            fileTags.Title = tags.Title;
            fileTags.Description = tags.Description;

            fileTags.Album = tags.Album;
            fileTags.DiscNumber = (uint?)tags.Disc;
            fileTags.Track = (uint?)tags.Track;
            fileTags.TotalTracks = (uint?)tags.TrackCount;

            fileTags.Year = tags.Year?.ToString();

            if (tags.Genres?.Any() == true)
            {
                fileTags.Genres = tags.Genres.ToArray();
            }

            if (config.ExtendedTagging)
            {
                fileTags.Conductor = tags.Conductor;
                fileTags.Performers = tags.Performers?.ToArray() ?? [];
                fileTags.PerformersRole = tags.PerformerRoles?.ToArray();
            }

            if (tags.Custom is not null && fileTags.SupportsCustomFields)
            {
                foreach (var (key, value) in tags.Custom)
                {
                    fileTags.SetCustomField(key, value);
                }
            }

            if (config.AppleTagging && file is Mp4File)
            {
                // TODO: apple tags - can't do currently as only string custom tags supported
                ui.SetStatus("Warning: Apple tagging not supported with experimental tagger", MessageType.Warning);
            }

            // if there is an image available and cover art is enabled
            if (!string.IsNullOrEmpty(metadata.CoverURL) && config.AddCoverArt)
            {
                var imgBytes = await coverArtFetcher.GetCoverArtAsync(metadata.CoverURL);
                if (imgBytes == null)
                {
                    ui.SetStatus(
                        $"Error: Failed to download cover art{(config.Verbose ? $"({metadata.CoverURL})" : "")}",
                        MessageType.Error
                    );

                    fileSuccess = false;
                }
                else
                {
                    fileTags.Pictures = [new Mp4Picture(imgBytes, true)];
                }
            }
            else if (string.IsNullOrEmpty(metadata.CoverURL) && config.AddCoverArt)
            {
                fileSuccess = false;
            }

            FileWriteResult? result = null;
            if (file is MatroskaFile mkv)
            {
                result = await mkv.SaveToFileAsync(taggingFile.Path, data);
            }
            else if (file is Mp4File mp4)
            {
                result = await mp4.SaveToFileAsync(taggingFile.Path, data);
            }

            if (result?.IsSuccess == true)
            {
                return fileSuccess;
            }

            ui.SetStatus($"Error: Failed to save file ({result?.Error})", MessageType.Error);
            return false;
        }
    }

    private bool TryReadFile(TaggingFile taggingFile, byte[] data, [NotNullWhen(true)] out IMediaFile? file)
    {
        var extension = Path.GetExtension(taggingFile.Path);
        if (extension.ToLower() is ".mkv")
        {
            var result = MatroskaFile.Read(data);
            if (result.IsSuccess)
            {
                file = result.File!;
            }
            else
            {
                ui.SetStatus($"Error: unable to read file ({result.Error})", MessageType.Error);
                file = null;
                return false;
            }
        }
        else if (extension.ToLower() is ".mp4" or ".m4v")
        {
            var result = Mp4File.Read(data);
            if (result.IsSuccess)
            {
                file = result.File!;
            }
            else
            {
                ui.SetStatus($"Error: unable to read file ({result.Error})", MessageType.Error);
                file = null;
                return false;
            }
        }
        else
        {
            ui.SetStatus($"Unsupported file extension: ${extension}", MessageType.Error);
            file = null;
            return false;
        }

        return true;
    }
}