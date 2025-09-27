using System.Text.Encodings.Web;
using System.Text.Json;
using AutoTag.Core.Files;

namespace AutoTag.Core.Config;

public interface IAutoTagConfigService
{
    Task<AutoTagConfig> LoadOrGenerateConfigAsync(string configPath);
    AutoTagConfig GetConfig();
    Task SaveToDiskAsync();
}

public class AutoTagConfigService(IUserInterface ui, IFileSystem fs) : IAutoTagConfigService
{
    private AutoTagConfig? Config { get; set; }
    private string? ConfigPath { get; set; }

    public async Task<AutoTagConfig> LoadOrGenerateConfigAsync(string configPath)
    {
        ConfigPath = configPath;
        
        if (!fs.Exists(configPath))
        {
            ui.DisplayMessage($"Generating new config file with default options: '{configPath}'", MessageType.Information);
            
            var configFile = new FileInfo(configPath);
            if (configFile.Directory != null)
            {
                fs.CreateDirectory(configFile.Directory);
            }

            Config = new AutoTagConfig();
            await WriteConfigToDiskAsync();
        }
        else
        {
            Config = await ReadConfigFromDiskAsync();
            await MigrateConfigVersionAsync();
        }
        
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

        await WriteConfigToDiskAsync();
    }

    private async Task<AutoTagConfig> ReadConfigFromDiskAsync()
    {
        if (string.IsNullOrEmpty(ConfigPath))
        {
            throw new InvalidOperationException("Config path cannot be null or empty");
        }
        
        AutoTagConfig? config = null;
        try
        {
            await using var stream = fs.OpenReadStream(ConfigPath);
            config = await JsonSerializer.DeserializeAsync<AutoTagConfig>(stream, JsonOptions);
        }
        catch (JsonException)
        {
           ui.DisplayMessage($"Error: Unable to parse config file '{ConfigPath}'", MessageType.Error);
        }
        catch (UnauthorizedAccessException)
        {
            ui.DisplayMessage($"Error: Config file '{ConfigPath}' not readable", MessageType.Error);
        }
        catch (Exception e)
        {
            ui.DisplayMessage($"Error reading config file '{ConfigPath}': {e.Message}", MessageType.Error);
        }
        finally
        {
            config ??= new AutoTagConfig();
        }

        return config;
    }

    private async Task MigrateConfigVersionAsync()
    {
        if (Config is null)
        {
            throw new InvalidOperationException($"Config has not been loaded, call {nameof(LoadOrGenerateConfigAsync)} first");
        }
        
        if (Config.ConfigVer != AutoTagConfig.CurrentVer)
        {
            if (Config.ConfigVer < 5 && Config.TVRenamePattern == "%1 - %2x%3 - %4")
            {
                Config.TVRenamePattern = "%1 - %2x%3:00 - %4";
            }

            // if config file outdated, update it with new options
            Config.ConfigVer = AutoTagConfig.CurrentVer;
            await WriteConfigToDiskAsync();
        }
    }
    
    private async Task WriteConfigToDiskAsync()
    {
        if (string.IsNullOrEmpty(ConfigPath) || Config is null)
        {
            throw new InvalidOperationException($"Config has not been loaded, call {nameof(LoadOrGenerateConfigAsync)} first");
        }
        
        try
        {
            await using var stream = fs.OpenWriteStream(ConfigPath);
            await JsonSerializer.SerializeAsync(stream, Config, JsonOptions);
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
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
}