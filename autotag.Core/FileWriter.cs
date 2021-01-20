using System;
using System.IO;
using System.Linq;
using System.Net;

namespace autotag.Core {
    public class FileWriter {
        public static bool write(string filePath, FileMetadata metadata, Action<string> setPath, Action<string, MessageType> setStatus, AutoTagConfig config) {
            bool fileSuccess = true;
            if (config.tagFiles) {
                if (invalidFilenameChars == null) {
                    invalidFilenameChars = Path.GetInvalidFileNameChars();

                    if (config.windowsSafe) {
                        invalidFilenameChars = invalidFilenameChars.Union(invalidNtfsChars).ToArray();
                    }
                }

                try {
                    TagLib.File file = TagLib.File.Create(filePath);

                    file.Tag.Title = metadata.Title;
                    file.Tag.Comment = metadata.Overview;
                    file.Tag.Genres = new string[] { (metadata.FileType == FileMetadata.Types.TV) ? "TVShows" : "Movie" };

                    if (metadata.FileType == FileMetadata.Types.TV) {
                        file.Tag.Album = metadata.SeriesName;
                        file.Tag.Disc = (uint) metadata.Season;
                        file.Tag.Track = (uint) metadata.Episode;
                    } else {
                        file.Tag.Year = (uint) metadata.Date.Year;
                    }

                    if (metadata.CoverFilename != "" && config.addCoverArt == true) { // if there is an image available and cover art is enabled
                        string downloadPath = Path.Combine(Path.GetTempPath(), "autotag");
                        string downloadFile = Path.Combine(downloadPath, metadata.CoverFilename);

                        if (!File.Exists(downloadFile)) { // only download file if it hasn't already been downloaded
                            if (!Directory.Exists(downloadPath)) {
                                Directory.CreateDirectory(downloadPath); // create temp directory
                            }

                            try {
                                using (WebClient client = new WebClient()) {
                                    client.DownloadFile(metadata.CoverURL, downloadFile); // download image
                                }
                                file.Tag.Pictures = new TagLib.Picture[] { new TagLib.Picture(downloadFile) { Filename = "cover.jpg" } };

                            } catch (WebException ex) {
                                if (config.verbose) {
                                    setStatus($"Error: Failed to download cover art ({ex.GetType().Name}: {ex.Message})", MessageType.Error);
                                } else {
                                    setStatus("Error: Failed to download cover art", MessageType.Error);
                                }
                                fileSuccess = false;
                            }
                        } else {
                            // overwrite default file name - allows software such as Icaros to display cover art thumbnails - default isn't compliant with Matroska guidelines
                            file.Tag.Pictures = new TagLib.Picture[] { new TagLib.Picture(downloadFile) { Filename = "cover.jpg" } };
                        }
                    } else if (String.IsNullOrEmpty(metadata.CoverFilename) && config.addCoverArt == true) {
                        fileSuccess = false;
                    }

                    file.Save();

                    if (fileSuccess == true) {
                        setStatus($"Successfully tagged file as {metadata}", MessageType.Information);
                    }

                } catch (Exception ex) {
                    if (config.verbose) {
                        setStatus($"Error: Failed to write tags to file ({ex.GetType().Name}: {ex.Message})", MessageType.Error);
                    } else {
                        setStatus("Error: Failed to write tags to file", MessageType.Error);
                    }
                    fileSuccess = false;
                }
            }

            if (config.renameFiles) {
                string newPath;
                if (config.mode == 0) {
                    newPath = Path.Combine(
                        Path.GetDirectoryName(filePath),
                        EscapeFilename(
                            String.Format(
                                GetTVRenamePattern(config),
                                metadata.SeriesName,
                                metadata.Season,
                                metadata.Episode.ToString("00"),
                                metadata.Title
                            ),
                            Path.GetFileNameWithoutExtension(filePath),
                            setStatus
                        )
                        + Path.GetExtension(filePath)
                    );
                } else {
                    newPath = Path.Combine(
                        Path.GetDirectoryName(filePath),
                        EscapeFilename(
                            String.Format(
                                GetMovieRenamePattern(config),
                                metadata.Title,
                                metadata.Date.Year
                            ),
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
                        if (config.verbose) {
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

        private static string GetTVRenamePattern(AutoTagConfig config) { // Get usable renaming pattern
            return config.tvRenamePattern.Replace("%1", "{0}").Replace("%2", "{1}").Replace("%3", "{2}").Replace("%4", "{3}");
        }

        private static string GetMovieRenamePattern(AutoTagConfig config) { // Get usable renaming pattern
            return config.movieRenamePattern.Replace("%1", "{0}").Replace("%2", "{1}");
        }

        private static char[] invalidFilenameChars { get; set; }

        private readonly static char[] invalidNtfsChars = { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };
    }
}