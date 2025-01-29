using System.Text.Encodings.Web;
using System.Text.Json;

namespace AutoTag.Core.Config;

public interface IAutoTagConfigService
{
    Task<AutoTagConfig> LoadOrGenerateConfigAsync(string configPath);
    AutoTagConfig GetConfig();
    Task SaveToDiskAsync();
}

public class AutoTagConfigService(IUserInterface ui) : IAutoTagConfigService
{
    private AutoTagConfig? Config { get; set; }
    private string? ConfigPath { get; set; }

    public async Task<AutoTagConfig> LoadOrGenerateConfigAsync(string configPath)
    {
        ConfigPath = configPath;
        
        if (!File.Exists(configPath))
        {
            ui.DisplayMessage($"Generating new config file with default options: '{configPath}'", MessageType.Information);
            
            var configFile = new FileInfo(configPath);
            
            configFile.Directory?.Create();

            await WriteConfigToDiskAsync(configPath, new AutoTagConfig());
        }

        var config = await ReadConfigFromDiskAsync(configPath);
        Config = config;

        await MigrateConfigVersionAsync(configPath, config);

        return Config;
    }

    public AutoTagConfig GetConfig()
        => Config
           ?? throw new InvalidOperationException(
               $"Config has not been loaded, call {nameof(LoadOrGenerateConfigAsync)} first");

    public async Task SaveToDiskAsync()
    {
        if (string.IsNullOrEmpty(ConfigPath) || Config is null)
        {
            throw new InvalidOperationException($"Config has not been loaded, call {nameof(LoadOrGenerateConfigAsync)} first");
        }

        await WriteConfigToDiskAsync(ConfigPath, Config);
    }

    private async Task<AutoTagConfig> ReadConfigFromDiskAsync(string configPath)
    {
        AutoTagConfig? config = null;
        try
        {
            config = JsonSerializer.Deserialize<AutoTagConfig>(await File.ReadAllTextAsync(configPath), JsonOptions)
                     ?? new AutoTagConfig();
        }
        catch (JsonException)
        {
           ui.DisplayMessage($"Error: Unable to parse config file '{configPath}'", MessageType.Error);
        }
        catch (UnauthorizedAccessException)
        {
            ui.DisplayMessage($"Error: Config file '{configPath}' not readable", MessageType.Error);
        }
        catch (Exception e)
        {
            ui.DisplayMessage($"Error reading config file '{configPath}': {e.Message}", MessageType.Error);
        }
        finally
        {
            config ??= new AutoTagConfig();
        }

        return config;
    }

    private async Task MigrateConfigVersionAsync(string configPath, AutoTagConfig config)
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

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
    private async Task WriteConfigToDiskAsync(string configPath, AutoTagConfig config)
    {
        try
        {
            await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(config, JsonOptions));
        }
        catch (UnauthorizedAccessException)
        {
            ui.DisplayMessage("Error: Config file not writeable", MessageType.Error);
        }
        catch (Exception)
        {
            ui.DisplayMessage("Error: Failed to write config file", MessageType.Error);
        }
    }
}