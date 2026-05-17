using API.Stories.Application.Ports;
using API.Stories.Application.Ports.Boundaries;
using API.Stories.Domain;
using Microsoft.Extensions.Logging;

namespace API.Stories.Application.Adapters.Services;

public sealed class HackerNewsStoryService(IHackerNewsHttpClient hackerNewsHttpClient,
                                           ICacheService cache,
                                           ILogger<HackerNewsStoryService> logger) : IHackerNewsStoryService
{
    private readonly IHackerNewsHttpClient __hackerNewsHttpClient = hackerNewsHttpClient;
    private readonly ICacheService __cache = cache;
    private readonly ILogger<HackerNewsStoryService> __logger = logger;

    private async IAsyncEnumerable<HackerNewsStory> GetStoriesByIdsAsync(
        int[] ids)
    {
        foreach (var id in ids)
        {
            var s = await __cache.GetAsync<HackerNewsStory>(HackerNewsStory.GetCacheKey(id));
            if (s is not null)
                yield return s;
        }
    }

    public async Task<Result<HackerNewsStory[]>> GetTokStoriesAsync(
        int topk,
        CancellationToken cancellationToken)
    {
        __logger.LogInformation("Starting top stories query for topk={TopK}", topk);

        var ires = await __hackerNewsHttpClient.GetBestStoriesAsync(cancellationToken);
        if (!ires)
        {
            __logger.LogWarning("Unable to retrieve best stories ids: {Error}", ires.Error);
            return ires.Error;
        }

        var heap = new HeapMinContainer<HackerNewsStory>(topk);
        var stories = GetStoriesByIdsAsync(ires);
        var options = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = 50,
        };

        await Parallel.ForEachAsync(stories, options, (s, token) =>
        {
            heap.Add(s, s.Score);
            return ValueTask.CompletedTask;
        });

        var result = heap.AsEnumerable().OrderByDescending(x => x.Score).ToArray();
        __logger.LogInformation(
            "Top stories query completed. Requested topk={TopK}, availableIds={IdsCount}, returnedStories={ReturnedCount}",
            topk,
            ires.Data.Length,
            result.Length);

        return result;
    }
}
