using System.Reflection;
using System.Text.Json;
using AutoTag.CLI.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Json;

namespace AutoTag.CLI;

public class RootCommand : AsyncCommand<RootCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RootCommandSettings cmdSettings)
    {
        if (cmdSettings.PrintVersion)
        {
            AnsiConsole.WriteLine(CLIInterface.GetVersion());
            return 0;
        }

        AutoTagSettings settings = await AutoTagSettings.LoadConfigAsync(cmdSettings.ConfigPath);
        cmdSettings.UpdateConfig(settings.Config);

        if (cmdSettings.PrintConfig)
        {
            AnsiConsole.Write(new JsonText(JsonSerializer.Serialize(settings.Config, PrintJsonOptions)));
            return 0;
        }

        if (cmdSettings.SetDefault)
        {
            await settings.SaveAsync();
        }

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddCoreServices(settings.Config, Keys.TMDBKey);

        builder.Services.AddSingleton(settings.Config);
        builder.Services.AddScoped<IUserInterface, CLIInterface>();

        using var host = builder.Build();

        var ui = (CLIInterface)host.Services.GetRequiredService<IUserInterface>();
        return await ui.RunAsync(cmdSettings.Paths);
    }

    private static readonly JsonSerializerOptions PrintJsonOptions = new()
    {
        WriteIndented = true
    };
}