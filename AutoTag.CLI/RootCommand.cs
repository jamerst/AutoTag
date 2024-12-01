using System.Reflection;
using System.Text.Json;
using AutoTag.CLI.Settings;

namespace AutoTag.CLI;

public class RootCommand : AsyncCommand<RootCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RootCommandSettings cmdSettings)
    {
        AutoTagSettings settings = await AutoTagSettings.LoadConfigAsync(cmdSettings.ConfigPath);
        cmdSettings.UpdateConfig(settings.Config);

        if (cmdSettings.PrintConfig)
        {
            Console.Write(JsonSerializer.Serialize(settings.Config, new JsonSerializerOptions() { WriteIndented = true }));
            return 0;
        }

        if (cmdSettings.SetDefault)
        {
            await settings.SaveAsync();
        }

        Console.WriteLine($"AutoTag v{Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}");
        Console.WriteLine("https://jtattersall.net");

        var processor = new Processor(settings.Config);

        return await processor.ProcessAsync(cmdSettings.Paths);
    }


}