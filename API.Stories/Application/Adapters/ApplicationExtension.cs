using System.Net;
using API.Stories.Application.Adapters.HttpClients;
using API.Stories.Application.Adapters.Services;
using API.Stories.Application.Ports;
using Flurl.Http.Configuration;
using StackExchange.Redis;

namespace API.Stories.Application.Adapters;

public static class ApplicationExtension
{
    public static T GetSettings<T>(this IConfiguration configuration) =>
        configuration.GetSection(typeof(T).Name).Get<T>();
    public static IServiceCollection ConfigureApplication(this IServiceCollection services,
                                                          IConfiguration configuration) =>
        services.AddSingleton<IHackerNewsStoryService, HackerNewsStoryService>()
                .AddHostedService<CachedStoryHostedService>()
                .AddSingleton<IConnectionMultiplexer>(
                    ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")))
                .AddSingleton<ICacheService, RedisService>()
                .ConfigureHttpClients(configuration);
    private static IServiceCollection ConfigureHttpClients(this IServiceCollection services,
                                                           IConfiguration configuration) =>
        services.AddSingleton<IFlurlClientCache, FlurlClientCache>()
            .AddSingleton<IHttpResiliencePipelineContext<IHackerNewsHttpClient>>(p =>
            {
                var settings = p.GetRequiredService<HackerNewsHttpClientSettings>();
                return new HttpResiliencePipelineContext<IHackerNewsHttpClient>(
                    settings.Retries,
                    response => response.StatusCode != (int)HttpStatusCode.OK);
            })
            .AddSingleton(configuration.GetSettings<HackerNewsHttpClientSettings>())
            .AddSingleton<IHackerNewsHttpClient, HackerNewsHttpClient>();

}