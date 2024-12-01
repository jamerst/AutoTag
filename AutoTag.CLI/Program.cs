var app = new CommandApp<AutoTag.CLI.RootCommand>()
    .WithDescription("Automatically tag and rename media files");

return await app.RunAsync(args);

/*
using System.Reflection;
using System.Text.Json;
using AutoTag.CLI.Settings;
 namespace AutoTag.CLI;
 class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCmd = new RootCommand("Automatically tag and rename media files");

        rootCmd.AddOptions<TaggingOptions>();
        rootCmd.AddOptions<RenameOptions>();
        rootCmd.AddOptions<MiscOptions>();

        var pathsArg = new Argument<IEnumerable<FileSystemInfo>>("paths", "Files or directories to process");
        rootCmd.AddArgument(pathsArg);

        rootCmd.SetHandler(
            HandleAsync,
            new CommandLineOptionBinder<TaggingOptions>(),
            new CommandLineOptionBinder<RenameOptions>(),
            new CommandLineOptionBinder<MiscOptions>(),
            pathsArg
        );

        return await rootCmd.InvokeAsync(args);
    }

    private static async Task<int> HandleAsync(
        TaggingOptions taggingOptions,
        RenameOptions renameOptions,
        MiscOptions miscOptions,
        IEnumerable<FileSystemInfo> paths)
    {
        AutoTagSettings settings = new AutoTagSettings(miscOptions.ConfigPath);

        taggingOptions.UpdateConfig(settings.Config);
        renameOptions.UpdateConfig(settings.Config);
        miscOptions.UpdateConfig(settings.Config);

        if (miscOptions.PrintConfig)
        {
            Console.Write(JsonSerializer.Serialize(settings.Config, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        if (miscOptions.SetDefault)
        {
            settings.Save();
        }

        Console.WriteLine($"AutoTag v{Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}");
        Console.WriteLine("https://jtattersall.net");

        var processor = new Processor(settings.Config);

        return await processor.ProcessAsync(paths);
    }
}*/
