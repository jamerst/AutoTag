using AutoTag.Core.Config;
using AutoTag.Core.Files;
using TagLib;
using TagLib.Mpeg4;
using File = TagLib.File;
using MatroskaTag = TagLib.Matroska.Tag;

namespace AutoTag.Core.Tagging;

public class TagLibTagger(IUserInterface ui, ICoverArtFetcher coverArtFetcher, AutoTagConfig config) : ITagger
{
    public async Task<bool> TagFileAsync(TaggingFile taggingFile, FileMetadata metadata)
    {
        var fileSuccess = true;

        File? file = null;
        try
        {
            using (file = File.Create(taggingFile.Path))
            {
                var tags = metadata.GetTags(config);

                file.Tag.Title = tags.Title;
                file.Tag.Description = tags.Description;

                if (file.IsMatroska())
                {
                    var custom = (MatroskaTag)file.GetTag(TagTypes.Matroska);
                    // workaround for https://github.com/mono/taglib-sharp/issues/263 - Tag.Album property writes to TITLE tag instead of ALBUM
                    // how has this still not been fixed??
                    custom.Set("ALBUM", null, tags.Album);

                    if (tags.Custom is not null)
                    {
                        foreach (var (key, value) in tags.Custom)
                        {
                            custom.Set(key, null, value);
                        }
                    }
                }
                else
                {
                    file.Tag.Album = tags.Album;
                }

                if (tags.Disc.HasValue)
                {
                    file.Tag.Disc = (uint)tags.Disc.Value;
                }

                if (tags.Track.HasValue)
                {
                    file.Tag.Track = (uint)tags.Track.Value;
                }

                if (tags.TrackCount.HasValue)
                {
                    file.Tag.TrackCount = (uint)tags.TrackCount.Value;
                }

                if (tags.Year.HasValue)
                {
                    file.Tag.Year = (uint)tags.Year.Value;
                }

                if (tags.Genres != null && tags.Genres.Any())
                {
                    file.Tag.Genres = tags.Genres.ToArray();
                }

                if (config.ExtendedTagging && file.IsMatroska())
                {
                    file.Tag.Conductor = tags.Conductor;
                    file.Tag.Performers = tags.Performers?.ToArray();
                    file.Tag.PerformersRole = tags.PerformerRoles?.ToArray();
                }

                // set extra tags because Apple is stupid and uses different tags for some reason
                // for a list of tags see https://kdenlive.org/en/project/adding-meta-data-to-mp4-video/
                if (config.AppleTagging && file.IsApple())
                {
                    var appleTags = (AppleTag)file.GetTag(TagTypes.Apple);

                    // Media Type - allows Apple software to recognise as a TV show or movie
                    // for a list of values see http://www.zoyinc.com/?p=1004
                    appleTags.SetData("stik", new ByteVector(tags.AppleMediaType),
                        (uint)AppleDataBox.FlagType.ContainsData);

                    // TV series name
                    if (!string.IsNullOrEmpty(tags.Album))
                    {
                        appleTags.SetText("tvsh", tags.Album);
                    }

                    if (tags.Disc >= 0)
                    {
                        // Season number - Apple tvsn is a 32-bit big-endian integer.
                        appleTags.SetData("tvsn", ByteVector.FromUInt((uint)tags.Disc),
                            (uint)AppleDataBox.FlagType.ContainsData);
                    }
                    else
                    {
                        ui.SetStatus("Warning: cannot add Apple tag for season number - value out of range",
                            MessageType.Warning);
                    }

                    if (tags.Track >= 0)
                    {
                        // Episode number - Apple tves is a 32-bit big-endian integer.
                        appleTags.SetData("tves", ByteVector.FromUInt((uint)tags.Track),
                            (uint)AppleDataBox.FlagType.ContainsData);
                    }
                    else
                    {
                        ui.SetStatus("Warning: cannot add Apple tag for episode number - value out of range",
                            MessageType.Warning);
                    }

                    if (tags.Disc.HasValue && tags.Track.HasValue)
                    {
                        // Sort name - allows older Apple software to sort correctly (sorts by title instead of season and episode on older devices)
                        appleTags.SetText("sonm", $"S{tags.Disc:00}E{tags.Track:00}");
                    }
                }

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
                        file.Tag.Pictures =
                            GetPicturesWithCover(file, new Picture(imgBytes) { Filename = "cover.jpg" });
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

    /// <summary>
    ///     Matroska stores every attachment as a picture, including the fonts that ASS/SSA subtitles need.
    ///     Replacing all pictures with the cover would delete them, so only image attachments are replaced there.
    /// </summary>
    private static IPicture[] GetPicturesWithCover(File file, IPicture cover)
        => file.IsMatroska()
            ? [cover, .. (file.Tag.Pictures ?? []).Where(p => p.MimeType?.StartsWith("image/") != true)]
            : [cover];
}