using System.Text.Json;
using System.Text.Encodings.Web;

namespace AutoTag.Core;
public class AutoTagSettings
{
    public AutoTagConfig Config;
    private string ConfigPath;
    private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public AutoTagSettings(string configPath)
    {
        ConfigPath = configPath;

        if (File.Exists(configPath))
        {
            try
            {
                Config = JsonSerializer.Deserialize<AutoTagConfig>(File.ReadAllText(configPath), jsonOptions)
                    ?? new AutoTagConfig();
            }
            catch (JsonException)
            {
                Console.Error.WriteLine($"Error parsing config file '{configPath}'");
            }
            catch (UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Config file '{configPath}' not readable");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error reading config file '{configPath}': {e.Message}");
            }
            finally
            {
                Config ??= new AutoTagConfig();
            }

            if (Config.ConfigVer != AutoTagConfig.CurrentVer)
            {
                if (Config.ConfigVer < 5 && Config.TVRenamePattern == "%1 - %2x%3 - %4")
                {
                    Config.TVRenamePattern = "%1 - %2x%3:00 - %4";
                }

                // if config file outdated, update it with new options
                Config.ConfigVer = AutoTagConfig.CurrentVer;
                Save();
            }
        }
        else
        {
            Console.WriteLine($"Generating new config file with default options: '{configPath}'");
            FileInfo configFile = new FileInfo(configPath);
            if (configFile.Directory != null)
            {
                configFile.Directory.Create();
            }

            try
            {
                File.WriteAllText(configPath, JsonSerializer.Serialize<AutoTagConfig>(new AutoTagConfig(), jsonOptions));
            }
            catch (UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Config file '{configPath}'not writeable");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error writing config file '{configPath}': {e.Message}");
            }

            try
            {
                Config = JsonSerializer.Deserialize<AutoTagConfig>(File.ReadAllText(configPath), jsonOptions)
                    ?? new AutoTagConfig();
            }
            catch (JsonException)
            {
                Console.Error.WriteLine($"Error parsing config file '{configPath}'");
            }
            catch (UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Config file '{configPath}' not readable");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error reading config file '{configPath}': {e.Message}");
            }
        }

        Config ??= new AutoTagConfig();
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize<AutoTagConfig>(Config, jsonOptions));
        }
        catch (UnauthorizedAccessException)
        {
            Console.Error.WriteLine("Config file not writeable");
        }
        catch (Exception)
        {
            Console.Error.WriteLine("Failed to write config file");
        }
    }
}