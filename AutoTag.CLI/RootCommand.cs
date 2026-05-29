using System.Text.Json;
using AutoTag.CLI.Settings;
using AutoTag.Core.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Json;

namespace AutoTag.CLI;

public class RootCommand(IAnsiConsole console) : AsyncCommand<RootCommandSettings>
{
    private static readonly JsonSerializerOptions PrintJsonOptions = new()
    {
        WriteIndented = true
    };

    protected override async Task<int> ExecuteAsync(CommandContext context, RootCommandSettings cmdSettings,
        CancellationToken cancellationToken)
    {
        if (cmdSettings.PrintVersion)
        {
            console.WriteLine(CLIInterface.GetVersion());
            return 0;
        }

        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.Services.AddCoreServices(ThisAssembly.Constants.TMDBApiKey);
        builder.Services.AddSingleton(console);
        builder.Services.AddScoped<IUserInterface, CLIInterface>();

        using var host = builder.Build();

        var configService = host.Services.GetRequiredService<IAutoTagConfigService>();
        var config = await configService.LoadOrGenerateConfigAsync(cmdSettings.ConfigPath);

        cmdSettings.UpdateConfig(config);

        if (cmdSettings.SetDefault)
        {
            await configService.SaveToDiskAsync();
        }

        if (cmdSettings.PrintConfig)
        {
            console.Write(new JsonText(JsonSerializer.Serialize(config, PrintJsonOptions)));
            return 0;
        }

        var ui = (CLIInterface)host.Services.GetRequiredService<IUserInterface>();
        return await ui.RunAsync(cmdSettings.Paths);
    }
}