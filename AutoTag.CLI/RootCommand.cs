using System.Text.Json;
using AutoTag.CLI.Settings;
using AutoTag.Core.Config;
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
        
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddCoreServices(Keys.TMDBKey);
        builder.Services.AddScoped<IUserInterface, CLIInterface>();

        using var host = builder.Build();

        var configService = host.Services.GetRequiredService<IAutoTagConfigService>();
        var config = await configService.LoadOrGenerateConfigAsync(cmdSettings.ConfigPath);
        
        cmdSettings.UpdateConfig(config);

        if (cmdSettings.PrintConfig)
        {
            AnsiConsole.Write(new JsonText(JsonSerializer.Serialize(config, PrintJsonOptions)));
            return 0;
        }

        if (cmdSettings.SetDefault)
        {
            await configService.SaveToDiskAsync();
        }

        var ui = (CLIInterface)host.Services.GetRequiredService<IUserInterface>();
        return await ui.RunAsync(cmdSettings.Paths);
    }

    private static readonly JsonSerializerOptions PrintJsonOptions = new()
    {
        WriteIndented = true
    };
}