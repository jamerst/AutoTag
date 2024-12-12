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
    public static void AddCoreServices(this IServiceCollection services, AutoTagConfig config, string apiKey)
    {
        services.AddScoped<IFileWriter, FileWriter>();
        services.AddScoped<ICoverArtFetcher, CoverArtFetcher>();

        if (config.IsTVMode())
        {
            services.AddScoped<IProcessor, TVProcessor>();
        }
        else
        {
            services.AddScoped<IProcessor, MovieProcessor>();
        }

        services.AddMemoryCache();
        
        services.AddHttpClient();
        services.RemoveAll<IHttpMessageHandlerBuilderFilter>(); // disable HttpClient logging - prints unwanted output to console
        
        services.AddScoped<TMDbClient>((_) => new(apiKey)
        {
            DefaultLanguage = config.Language,
            DefaultImageLanguage = config.Language
        });
        services.AddScoped<ITMDBService, TMDBService>();
    }

    public static bool IsError(this MessageType type)
        => (type & MessageType.Error) == MessageType.Error;

    public static bool IsWarning(this MessageType type)
        => (type & MessageType.Warning) == MessageType.Warning;

    public static bool IsInformation(this MessageType type)
        => (type & MessageType.Information) == MessageType.Information;

    public static bool IsLog(this MessageType type)
        => (type & MessageType.Log) == MessageType.Log;
}