using System.Text.Json;
using System.Text.Encodings.Web;

namespace AutoTag.Core;
public class AutoTagSettings
{
    public AutoTagConfig Config { get; set; } = null!;
    private string ConfigPath { get; set; } = null!;
    
    private AutoTagSettings() {}

    public static async Task<AutoTagSettings> LoadConfigAsync(string configPath)
    {
        var settings = new AutoTagSettings
        {
            ConfigPath = configPath
        };

        if (!File.Exists(configPath))
        {
            Console.WriteLine($"Generating new config file with default options: '{configPath}'");
            FileInfo configFile = new FileInfo(configPath);
            if (configFile.Directory != null)
            {
                configFile.Directory.Create();
            }

            await WriteConfigToDiskAsync(configPath, new AutoTagConfig());
        }

        var config = await ReadConfigFromDiskAsync(configPath);
        settings.Config = config;

        await MigrateConfigVersionAsync(configPath, config);

        return settings;
    }

    private static async Task<AutoTagConfig> ReadConfigFromDiskAsync(string configPath)
    {
        AutoTagConfig? config = null;
        try
        {
            config = JsonSerializer.Deserialize<AutoTagConfig>(await File.ReadAllTextAsync(configPath), JsonOptions)
                     ?? new AutoTagConfig();
        }
        catch (JsonException)
        {
           Console.WriteLine($"Error parsing config file '{configPath}'");
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine($"Config file '{configPath}' not readable");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error reading config file '{configPath}': {e.Message}");
        }
        finally
        {
            config ??= new AutoTagConfig();
        }

        return config;
    }

    private static async Task MigrateConfigVersionAsync(string configPath, AutoTagConfig config)
    {
        if (config.ConfigVer != AutoTagConfig.CurrentVer)
        {
            if (config.ConfigVer < 5 && config.TVRenamePattern == "%1 - %2x%3 - %4")
            {
                config.TVRenamePattern = "%1 - %2x%3:00 - %4";
            }

            // if config file outdated, update it with new options
            config.ConfigVer = AutoTagConfig.CurrentVer;
            await WriteConfigToDiskAsync(configPath, config);
        }
    }

    public Task SaveAsync() => WriteConfigToDiskAsync(ConfigPath, Config);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
    private static async Task WriteConfigToDiskAsync(string configPath, AutoTagConfig config)
    {
        try
        {
            await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(config, JsonOptions));
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("Config file not writeable");
        }
        catch (Exception)
        {
            Console.WriteLine("Failed to write config file");
        }
    }
}