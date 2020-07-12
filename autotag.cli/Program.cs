using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using McMaster.Extensions.CommandLineUtils;

using autotag.Core;

namespace autotag.cli {
    [Command(UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.CollectAndContinue)]
    class Program {
        private List<TaggingFile> files { get; set; } = new List<TaggingFile>();
        private int index;
        private int lastIndex = -1;
        private bool success = true;

        private AutoTagSettings settings;

        private async Task OnExecuteAsync() {
            if (version) {
                Console.WriteLine(Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);
                Environment.Exit(0);
            }

            Console.WriteLine($"AutoTag v{Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion}\n");
            settings = new AutoTagSettings(configPath);

            if (tv) {
                settings.config.mode = AutoTagConfig.Modes.TV;
            } else if (movie) {
                settings.config.mode = AutoTagConfig.Modes.Movie;
            }
            if (noRename) {
                settings.config.renameFiles = false;
            }
            if (noTag) {
                settings.config.tagFiles = false;
            }
            if (noCoverArt) {
                settings.config.addCoverArt = false;
            }
            if (manualMode) {
                settings.config.manualMode = true;
            }
            if (verbose) {
                settings.config.verbose = true;
            }
            if (setDefault) {
                settings.Save();
            }

            if (RemainingArguments.Length == 0) {
                Console.Error.WriteLine("No files provided");
                Environment.Exit(1);
            }

            IProcessor processor;
            if (settings.config.mode == AutoTagConfig.Modes.TV) {
                processor = new TVProcessor("TQLC3N5YDI1AQVJF");
            } else {
                processor = new MovieProcessor("b342b6005f86daf016533bf0b72535bc");
            }

            AddFiles(RemainingArguments);
            files.Sort((x,y) => x.Path.CompareTo(y.Path));

            Action<string> setPath = p => { return; };
            Action<string, bool> setStatus = (s, e) => SetStatus(s, e);

            Func<List<Tuple<string, string>>, int> choose = (results) => ChooseResult(results);

            for (index = 0; index < files.Count; index++) {
                success = success & await processor.process(files[index].Path, setPath, setStatus, choose, settings.config);
            }

            Console.ResetColor();

            if (success) {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n\nAll files successfully processed.");
                Console.ResetColor();
                Environment.Exit(0);
            } else {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.Error.WriteLine("\n\nErrors encountered for some files:");
                foreach (TaggingFile file in files.Where(f => !f.Success)) {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.Error.WriteLine($"{file.Path}:");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine($"    {file.Status}\n");
                }
                Console.ResetColor();
                Environment.Exit(1);
            }
        }

        private void SetStatus(string status, bool error) {
            if (error && !files[index].Success) {
                files[index].Status += Environment.NewLine + status;
            } else if (error) {
                success = false;
                files[index].Success = false;
                Console.ForegroundColor = ConsoleColor.Red;
                files[index].Status = status;
            } else if (files[index].Success) {
                files[index].Status = status;
            }

            if (index > lastIndex) {
                lastIndex++;
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"\n{files[index].Path}:");
                Console.ResetColor();
            } else {
                Console.WriteLine($"    {files[index].Status}");
            }

        }

        private int ChooseResult(List<Tuple<string, string>> results) {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("    Please choose a series:");
            Console.ResetColor();
            for (int i = 0; i < results.Count; i++) {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"        {i}: {results[i].Item1} ({results[i].Item2})");
            }
            Console.ResetColor();

            return InputResult(results.Count);
        }

        private int InputResult(int count) {
            Console.Write($"    Choose an option [0-{count - 1}]: ");
            string choice = Console.ReadLine();

            int chosen;
            if (int.TryParse(choice, out chosen) && chosen >= 0 && chosen < count) {
                return chosen;
            } else {
                return InputResult(count);
            }
        }

        private void AddFiles(string[] paths) {
            foreach (string path in paths) {
                if (File.Exists(path) || Directory.Exists(path)) {
                    if (File.GetAttributes(path).HasFlag(FileAttributes.Directory)) {
                        if (settings.config.verbose) Console.WriteLine($"Adding all files in directory '{path}'");
                        AddFiles(Directory.GetFileSystemEntries(path));
                    } else if (!files.Any(f => Path.GetFullPath(f.Path) == Path.GetFullPath(path))) {
                        if (supportedExtensions.Contains(Path.GetExtension(path))) {
                            // add file if not already added and has a supported file extension
                            files.Add(new TaggingFile { Path = path });
                            if (settings.config.verbose) Console.WriteLine($"Adding file '{path}'");
                        } else {
                            if (settings.config.verbose) Console.Error.WriteLine($"Unsupported file: '{path}'");
                        }
                    }
                } else {
                    Console.Error.WriteLine($"Path not found: {path}");
                }
            }
        }

        static void Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        [Option(Description = "TV tagging mode")]
        private bool tv { get; set; }

        [Option(Description = "Movie tagging mode")]
        private bool movie { get; set; }

        [Option(Description = "Specify config file to load")]
        private string configPath { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "autotag",
            "conf.json"
        );

        [Option("--no-rename", "Disable file renaming", CommandOptionType.NoValue)]
        private bool noRename { get; set; }

        [Option("--no-tag", "Disable file tagging", CommandOptionType.NoValue)]
        private bool noTag { get; set; }

        [Option("--no-cover", "Disable cover art tagging", CommandOptionType.NoValue)]
        private bool noCoverArt { get; set; }

        [Option("--manual", "Manually choose the series to tag", CommandOptionType.NoValue)]
        private bool manualMode { get; set; }

        [Option(Description = "Enable verbose output mode")]
        private bool verbose { get; set; }

        [Option("--set-default", "Set the current arguments as the default", CommandOptionType.NoValue)]
        private bool setDefault { get; set; }

        [Option("--version", "Print version number and exit", CommandOptionType.NoValue)]
        private bool version { get; set; }
        private string[] RemainingArguments { get; }

        private static readonly string[] supportedExtensions = new string[] { ".mp4", ".m4v", ".mkv" };
    }
}
