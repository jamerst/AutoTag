using System;
using System.IO;
using System.Text.Json;

namespace autotag.Core {
    public class AutoTagSettings {
        public AutoTagConfig config;
        public string configPath;
        private readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions {
            WriteIndented = true
        };

        public AutoTagSettings(string configPath) {
            this.configPath = configPath;
            if (File.Exists(configPath)) {
                try {
                    config = JsonSerializer.Deserialize<AutoTagConfig>(File.ReadAllText(configPath));
                } catch (JsonException) {
                    Console.Error.WriteLine($"Error parsing config file '{configPath}'");
                } catch (UnauthorizedAccessException) {
                    Console.Error.WriteLine($"Config file '{configPath}' not readable");
                } catch (Exception e) {
                    Console.Error.WriteLine($"Error reading config file '{configPath}': {e.Message}");
                }
            } else {
                Console.WriteLine($"Generating new config file with default options '{configPath}'");
                FileInfo configFile = new FileInfo(configPath);
                configFile.Directory.Create();

                try {
                    File.WriteAllText(configPath, JsonSerializer.Serialize<AutoTagConfig>(new AutoTagConfig(), jsonOptions));
                } catch (UnauthorizedAccessException) {
                    Console.Error.WriteLine($"Config file '{configPath}'not writeable");
                } catch (Exception e) {
                    Console.Error.WriteLine($"Error writing config file '{configPath}': {e.Message}");
                }

                try {
                    config = JsonSerializer.Deserialize<AutoTagConfig>(File.ReadAllText(configPath));
                } catch (JsonException) {
                    Console.Error.WriteLine($"Error parsing config file '{configPath}'");
                } catch (UnauthorizedAccessException) {
                    Console.Error.WriteLine($"Config file '{configPath}' not readable");
                } catch (Exception e) {
                    Console.Error.WriteLine($"Error reading config file '{configPath}': {e.Message}");
                }
            }
        }

        public void Save() {
            try {
                    File.WriteAllText(configPath, JsonSerializer.Serialize<AutoTagConfig>(config, jsonOptions));
                } catch (UnauthorizedAccessException) {
                    Console.Error.WriteLine("Config file not writeable");
                } catch (Exception) {
                    Console.Error.WriteLine("Failed to write config file");
                }
        }
    }
}