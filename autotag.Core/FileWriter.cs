using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
namespace autotag.Core {
    public class FileWriter {
        private readonly static HttpClient _client = new HttpClient();
        private static Dictionary<string, byte[]> _images = new Dictionary<string, byte[]>();

        public static async Task<bool> Write(string filePath, FileMetadata metadata, Action<string> setPath, Action<string, MessageType> setStatus, AutoTagConfig config) {
            bool fileSuccess = true;
            if (config.TagFiles) {
                if (invalidFilenameChars == null) {
                    invalidFilenameChars = Path.GetInvalidFileNameChars();

                    if (config.WindowsSafe) {
                        invalidFilenameChars = invalidFilenameChars.Union(invalidNtfsChars).ToArray();
                    }
                }

                TagLib.File file = null;
                try {
                    file = TagLib.File.Create(filePath);

                    file.Tag.Title = metadata.Title;
                    file.Tag.Description = metadata.Overview;

                    if (metadata.Genres != null && metadata.Genres.Any()) {
                        file.Tag.Genres = metadata.Genres;
                    }

                    if (config.ExtendedTagging && file.MimeType == "video/x-matroska") {
                        if (metadata.FileType == FileMetadata.Types.TV && metadata.Id.HasValue) {
                            var custom = (TagLib.Matroska.Tag)file.GetTag(TagLib.TagTypes.Matroska);
                            custom.Set("TMDB", "", $"tv/{metadata.Id}");
                        }

                        file.Tag.Conductor = metadata.Director;
                        file.Tag.Performers = metadata.Actors;
                        file.Tag.PerformersRole = metadata.Characters;
                    }

                    if (metadata.FileType == FileMetadata.Types.TV) {
                        file.Tag.Album = metadata.SeriesName;
                        file.Tag.Disc = (uint) metadata.Season;
                        file.Tag.Track = (uint) metadata.Episode;
                        file.Tag.TrackCount = (uint) metadata.SeasonEpisodes;

                        // set extra tags because Apple is stupid and uses different tags for some reason
                        // for a list of tags see https://kdenlive.org/en/project/adding-meta-data-to-mp4-video/
                        if (config.AppleTagging && file.MimeType.EndsWith("/mp4")) {
                            var appleTags = (TagLib.Mpeg4.AppleTag) file.GetTag(TagLib.TagTypes.Apple);

                            // Media Type - allows Apple software to recognise as a TV show
                            // for a list of values see http://www.zoyinc.com/?p=1004
                            appleTags.SetData("stik", new TagLib.ByteVector(_stikTVShow), (uint) TagLib.Mpeg4.AppleDataBox.FlagType.ContainsData);

                            // Series
                            appleTags.SetText("tvsh", metadata.SeriesName);

                            if (metadata.Season >= byte.MinValue && metadata.Season <= byte.MaxValue) {
                                // Season number
                                appleTags.SetData("tvsn", new TagLib.ByteVector((byte) metadata.Season), (uint) TagLib.Mpeg4.AppleDataBox.FlagType.ContainsData);
                            } else {
                                setStatus($"Warning: cannot add Apple tag for season number - value out of range", MessageType.Warning);
                            }

                            if (metadata.Episode >= byte.MinValue && metadata.Episode <= byte.MaxValue) {
                                // Episode number
                                appleTags.SetData("tves", new TagLib.ByteVector((byte) metadata.Episode), (uint) TagLib.Mpeg4.AppleDataBox.FlagType.ContainsData);
                            } else {
                                setStatus($"Warning: cannot add Apple tag for episode number - value out of range", MessageType.Warning);
                            }
                        }
                    } else {
                        file.Tag.Year = (uint) metadata.Date.Year;

                        if (config.AppleTagging && file.MimeType.EndsWith("/mp4")) {
                            var appleTags = (TagLib.Mpeg4.AppleTag) file.GetTag(TagLib.TagTypes.Apple);

                            // Media Type - allows Apple software to recognise as a movie
                            appleTags.SetData("stik", new TagLib.ByteVector(_stikMovie), (uint) TagLib.Mpeg4.AppleDataBox.FlagType.ContainsData);
                        }
                    }

                    if (!string.IsNullOrEmpty(metadata.CoverFilename) && config.AddCoverArt == true) { // if there is an image available and cover art is enabled
                        if (!_images.ContainsKey(metadata.CoverFilename)) {
                            var response = await _client.GetAsync(metadata.CoverURL, HttpCompletionOption.ResponseHeadersRead);
                            if (response.IsSuccessStatusCode) {
                                _images[metadata.CoverFilename] = await response.Content.ReadAsByteArrayAsync();
                            } else {
                                if (config.Verbose) {
                                    setStatus($"Error: failed to download cover art ({(int) response.StatusCode}:{metadata.CoverURL})", MessageType.Error);
                                } else {
                                    setStatus($"Error: failed to download cover art", MessageType.Error);
                                }
                                fileSuccess = false;
                            }
                        }

                        if (_images.TryGetValue(metadata.CoverFilename, out byte[] imgBytes)) {
                            file.Tag.Pictures = new TagLib.Picture[] { new TagLib.Picture(imgBytes) { Filename = "cover.jpg" } };
                        }
                    } else if (string.IsNullOrEmpty(metadata.CoverFilename) && config.AddCoverArt == true) {
                        fileSuccess = false;
                    }

                    file.Save();

                    if (fileSuccess == true) {
                        setStatus($"Successfully tagged file as {metadata}", MessageType.Information);
                    }

                } catch (Exception ex) {
                    if (config.Verbose) {
                        if (file != null && file.CorruptionReasons.Any()) {
                            setStatus($"Error: Failed to write tags to file ({ex.GetType().Name}: {ex.Message}; CorruptionReasons: {string.Join(", ", file.CorruptionReasons)})", MessageType.Error);
                        } else {
                            setStatus($"Error: Failed to write tags to file ({ex.GetType().Name}: {ex.Message}", MessageType.Error);
                        }
                    } else {
                        setStatus("Error: Failed to write tags to file", MessageType.Error);
                    }
                    fileSuccess = false;
                }
            }

            if (config.RenameFiles) {
                string newPath;
                if (config.Mode == 0) {
                    newPath = Path.Combine(
                        Path.GetDirectoryName(filePath),
                        EscapeFilename(
                            GetTVFileName(config, metadata.SeriesName, metadata.Season, metadata.Episode, metadata.Title),
                            Path.GetFileNameWithoutExtension(filePath),
                            setStatus
                        )
                        + Path.GetExtension(filePath)
                    );
                } else {
                    newPath = Path.Combine(
                        Path.GetDirectoryName(filePath),
                        EscapeFilename(
                            GetMovieFileName(config, metadata.Title, metadata.Date.Year),
                            Path.GetFileNameWithoutExtension(filePath),
                            setStatus
                        )
                        + Path.GetExtension(filePath)
                    );
                }

                if (filePath != newPath) {
                    try {
                        if (File.Exists(newPath)) {
                            setStatus("Error: Could not rename - file already exists", MessageType.Error);
                            fileSuccess = false;
                        } else {
                            File.Move(filePath, newPath);
                            setPath(newPath);
                            setStatus($"Successfully renamed file to '{Path.GetFileName(newPath)}'", MessageType.Information);
                        }
                    } catch (Exception ex) {
                        if (config.Verbose) {
                            setStatus($"Error: Failed to rename file ({ex.GetType().Name}: {ex.Message})", MessageType.Error);
                        } else {
                            setStatus("Error: Failed to rename file", MessageType.Error);
                        }
                        fileSuccess = false;
                    }
                }
            }

            return fileSuccess;
        }

        private static string EscapeFilename(string filename, string oldFilename, Action<string, MessageType> setStatus) {
            string result = String.Join("", filename.Split(invalidFilenameChars));
            if (result != oldFilename && result.Length != filename.Length) {
                setStatus("Warning: Invalid characters in file name, automatically removing", MessageType.Warning);
            }

            return result;
        }

        private static string GetTVFileName(AutoTagConfig config, string series, int season, int episode, string title) {
            return _renameRegex.Replace(config.TVRenamePattern, (m) => {
                switch (m.Groups["num"].Value) {
                    case "1": return series;
                    case "2": return FormatRenameNumber(m, season);
                    case "3": return FormatRenameNumber(m, episode);
                    case "4": return title;
                    default: return m.Value;
                }
            });
        }

        private static string GetMovieFileName(AutoTagConfig config, string title, int year) {
            return _renameRegex.Replace(config.MovieRenamePattern, (m) => {
                switch (m.Groups["num"].Value) {
                    case "1": return title;
                    case "2": return FormatRenameNumber(m, year);
                    default: return m.Value;
                }
            });
        }

        private readonly static Regex _renameRegex = new Regex(@"%(?<num>\d+)(?:\:(?<format>[0#]+))?");

        private static string FormatRenameNumber(Match match, int value) {
            if (match.Groups.ContainsKey("format")) {
                return value.ToString(match.Groups["format"].Value);
            } else {
                return value.ToString();
            }
        }

        private static char[] invalidFilenameChars { get; set; }

        private readonly static char[] invalidNtfsChars = { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };

        private const byte _stikTVShow = 10;
        private const byte _stikMovie = 9;
    }
}