using System.Net;
using API.Stories.Application.Ports;
using API.Stories.Application.Ports.Boundaries;
using API.Stories.Domain;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Logging;

namespace API.Stories.Application.Adapters.HttpClients;

public sealed record HackerNewsHttpClientSettings(
    string Host,
    int Retries,
    int TimeoutMs);

public sealed class HackerNewsHttpClient(
    HackerNewsHttpClientSettings settings,
    IHttpResiliencePipelineContext<IHackerNewsHttpClient> resiliencePipeline,
    IFlurlClientCache flurlClientCache,
    ILogger<HackerNewsHttpClient> logger) : IHackerNewsHttpClient
{
    private readonly IFlurlClient __flurlClient =
        flurlClientCache.GetOrAdd(
            nameof(HackerNewsHttpClientSettings),
            settings.Host, cfg =>
                cfg.AllowAnyHttpStatus()
                   .WithSettings(a =>
                        a.Timeout = TimeSpan.FromMilliseconds(settings.TimeoutMs)));

    private readonly IHttpResiliencePipelineContext __resiliencePipeline =
        resiliencePipeline;
    private readonly ILogger<HackerNewsHttpClient> __logger = logger;

    public async Task<Result<int[]>> GetBestStoriesAsync(
        CancellationToken cancellationToken)
    {
        var response = await __resiliencePipeline.Pipeline.ExecuteAsync(async token =>
              await __flurlClient.Request("v0/beststories.json")
                                 .GetAsync(cancellationToken: token), cancellationToken);

        if (response.StatusCode != (int)HttpStatusCode.OK)
        {
            var error = await response.GetStringAsync();
            __logger.LogWarning(
                "Hacker News best stories request failed with status {StatusCode}: {Error}",
                response.StatusCode,
                error);
            return error;
        }

        return await response.GetJsonAsync<int[]>();
    }
    public async Task<Result<HackerNewsStory>> GetHackerNewsStoryAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var response = await __resiliencePipeline.Pipeline.ExecuteAsync(async token =>
                await __flurlClient.Request("v0/item", $"{id}.json")
                                    .GetAsync(cancellationToken: token), cancellationToken);

        if (response.StatusCode != (int)HttpStatusCode.OK)
        {
            var error = await response.GetStringAsync();
            __logger.LogWarning(
                "Hacker News story request failed for id {StoryId} with status {StatusCode}: {Error}",
                id,
                response.StatusCode,
                error);
            return error;
        }

        return await response.GetJsonAsync<HackerNewsStory>();
    }
}
