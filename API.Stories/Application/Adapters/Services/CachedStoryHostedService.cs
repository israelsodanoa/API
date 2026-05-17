using API.Stories.Application.Ports;
using API.Stories.Application.Ports.Boundaries;
using Microsoft.Extensions.Logging;

namespace API.Stories.Application.Adapters.Services;

public sealed class CachedStoryHostedService(IHackerNewsHttpClient hackerNewsHttpClient,
                                             ICacheService cache,
                                             ILogger<CachedStoryHostedService> logger) : IHostedService
{
    private readonly IHackerNewsHttpClient __hackerNewsHttpClient = hackerNewsHttpClient;
    private readonly ICacheService __cache = cache;
    private readonly ILogger<CachedStoryHostedService> __logger = logger;
    private Timer __timer;

    private async Task LoadCacheStories(CancellationToken cancellationToken)
    {
        try
        {
            var ires = await __hackerNewsHttpClient.GetBestStoriesAsync(cancellationToken);
            if (!ires)
            {
                __logger.LogWarning("Unable to refresh stories cache: {Error}", ires.Error);
                return;
            }

            var options = new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = 50,
            };

            var cachedStories = 0;
            await Parallel.ForEachAsync(ires.Data, options, async (id, token) =>
            {
                var s = await __hackerNewsHttpClient.GetHackerNewsStoryAsync(id, token);
                if (s)
                {
                    await __cache.SetAsync(s.Data, HackerNewsStory.GetCacheKey(s.Data.Id));
                    Interlocked.Increment(ref cachedStories);
                }
            });

            __logger.LogInformation(
                "Stories cache refresh completed. SourceIds={IdsCount}, CachedStories={CachedStories}",
                ires.Data.Length,
                cachedStories);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            __logger.LogInformation("Stories cache refresh canceled");
        }
        catch (Exception ex)
        {
            __logger.LogError(ex, "Stories cache refresh failed");
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        __logger.LogInformation("Starting stories cache hosted service");
        await LoadCacheStories(cancellationToken);

        __timer = new Timer(
            _ => _ = LoadCacheStories(cancellationToken),
            null,
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(10));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        __logger.LogInformation("Stopping stories cache hosted service");
        __timer?.Dispose();
        __timer = null;
        return Task.CompletedTask;
    }
}
