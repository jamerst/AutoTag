using AutoTag.Core.Config;

namespace AutoTag.CLI.Settings;

public partial class RootCommandSettings : CommandSettings
{
    [CommandArgument(0, "[paths]")]
    [Description("Files or directories to process")]
    public string[]? PathStrings { get; init; }

    public IEnumerable<FileSystemInfo> Paths { get; init; }

    public RootCommandSettings(string[]? pathStrings, string? configPath)
    {
        Paths = pathStrings?.Select(PathToFileSystemInfo).ToList() ?? [];
        ConfigPath = configPath ?? GetDefaultConfigPath();
    }

    public void UpdateConfig(AutoTagConfig config)
    {
        SetTaggingOptions(config);
        SetRenameOptions(config);
        SetMiscOptions(config);
    }

    private static FileSystemInfo PathToFileSystemInfo(string path)
    {
        bool isDirectory;
        try
        {
            isDirectory = File.GetAttributes(path).HasFlag(FileAttributes.Directory);
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
        {
            // handle gracefully, CLI will display error message when it checks if file exists
            isDirectory = false;
        }
        
        return isDirectory
            ? new DirectoryInfo(path)
            : new FileInfo(path);
    }

    private static string GetDefaultConfigPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "autotag",
        "conf.json"
    );
}