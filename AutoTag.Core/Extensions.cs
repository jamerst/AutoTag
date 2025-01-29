using System.Diagnostics.CodeAnalysis;
using AutoTag.Core.Config;
using AutoTag.Core.Files;
using AutoTag.Core.Movie;
using AutoTag.Core.TMDB;
using AutoTag.Core.TV;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using TMDbLib.Client;

namespace AutoTag.Core;

public static class Extensions
{
    public static void AddCoreServices(this IServiceCollection services, string apiKey)
    {
        services.AddSingleton<IAutoTagConfigService, AutoTagConfigService>();
        services.AddSingleton<AutoTagConfig>(serviceProvider =>
            serviceProvider.GetRequiredService<IAutoTagConfigService>().GetConfig());

        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IFileFinder, FileFinder>();
        
        services.AddSingleton<IFileWriter, FileWriter>();
        
        services.AddScoped<ICoverArtFetcher, CoverArtFetcher>();

        services.AddKeyedScoped<IProcessor, TVProcessor>(Mode.TV);
        services.AddKeyedScoped<IProcessor, MovieProcessor>(Mode.Movie);

        services.AddMemoryCache();
        
        services.AddHttpClient();
        services.RemoveAll<IHttpMessageHandlerBuilderFilter>(); // disable HttpClient logging - prints unwanted output to console
        
        services.AddScoped<TMDbClient>(serviceProvider =>
        {
            var configService = serviceProvider.GetRequiredService<IAutoTagConfigService>();
            var config = configService.GetConfig();
            
            return new(apiKey)
            {
                DefaultLanguage = config.Language,
                DefaultImageLanguage = config.Language
            };
        });
        services.AddScoped<ITMDBService, TMDBService>();

        services.AddScoped<ITVCache, TVCache>();
    }

    public static bool IsError(this MessageType type)
        => (type & MessageType.Error) == MessageType.Error;

    public static bool IsWarning(this MessageType type)
        => (type & MessageType.Warning) == MessageType.Warning;

    public static bool IsInformation(this MessageType type)
        => (type & MessageType.Information) == MessageType.Information;

    public static bool IsLog(this MessageType type)
        => (type & MessageType.Log) == MessageType.Log;

    public static bool TryFind<T>(this List<T> list, Predicate<T> match, [NotNullWhen(true)] out T? item)
    {
        item = list.Find(match);

        return item != null;
    }
}