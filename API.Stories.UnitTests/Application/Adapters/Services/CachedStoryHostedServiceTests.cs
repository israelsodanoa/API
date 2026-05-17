using API.Stories.Application.Adapters.Services;
using API.Stories.Application.Ports;
using API.Stories.Application.Ports.Boundaries;
using API.Stories.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace API.Stories.UnitTests.Application.Adapters.Services;

public sealed class CachedStoryHostedServiceTests
{
    [Fact]
    public async Task StartAsync_CachesStoriesFromBestStoryIds()
    {
        var firstTick = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var allCached = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var story1Key = HackerNewsStory.GetCacheKey(1);
        var story2Key = HackerNewsStory.GetCacheKey(2);
        var observedKeys = new HashSet<string>();
        var observedLock = new object();

        var httpClient = new Mock<IHackerNewsHttpClient>(MockBehavior.Strict);
        httpClient.Setup(x => x.GetBestStoriesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => firstTick.TrySetResult(true))
            .ReturnsAsync(new Result<int[]>(new[] { 1, 2 }));

        httpClient.Setup(x => x.GetHackerNewsStoryAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<HackerNewsStory>(CreateStory(1, 10)));

        httpClient.Setup(x => x.GetHackerNewsStoryAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<HackerNewsStory>(CreateStory(2, 20)));

        var cache = new Mock<ICacheService>(MockBehavior.Strict);
        cache.Setup(x => x.SetAsync(It.IsAny<HackerNewsStory>(), It.IsAny<string>()))
            .Callback<HackerNewsStory, string>((_, key) =>
            {
                lock (observedLock)
                {
                    observedKeys.Add(key);
                    if (observedKeys.Contains(story1Key) && observedKeys.Contains(story2Key))
                        allCached.TrySetResult(true);
                }
            })
            .ReturnsAsync((HackerNewsStory story, string _) => story);

        var sut = new CachedStoryHostedService(
            httpClient.Object,
            cache.Object,
            NullLogger<CachedStoryHostedService>.Instance);
        var stopped = false;

        await sut.StartAsync(CancellationToken.None);

        try
        {
            await firstTick.Task.WaitAsync(TimeSpan.FromSeconds(2));
            await allCached.Task.WaitAsync(TimeSpan.FromSeconds(2));

            await sut.StopAsync(CancellationToken.None);
            stopped = true;

            cache.Verify(x => x.SetAsync(It.IsAny<HackerNewsStory>(), story1Key), Times.AtLeastOnce);
            cache.Verify(x => x.SetAsync(It.IsAny<HackerNewsStory>(), story2Key), Times.AtLeastOnce);
        }
        finally
        {
            if (!stopped)
                await sut.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task StartAsync_DoesNothingWhenBestStoriesCallFails()
    {
        var firstTick = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var httpClient = new Mock<IHackerNewsHttpClient>(MockBehavior.Strict);
        httpClient.Setup(x => x.GetBestStoriesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => firstTick.TrySetResult(true))
            .ReturnsAsync((Result<int[]>)"failed");

        var cache = new Mock<ICacheService>(MockBehavior.Strict);

        var sut = new CachedStoryHostedService(
            httpClient.Object,
            cache.Object,
            NullLogger<CachedStoryHostedService>.Instance);

        await sut.StartAsync(CancellationToken.None);

        try
        {
            await firstTick.Task.WaitAsync(TimeSpan.FromSeconds(2));
            await Task.Delay(150);

            cache.Verify(x => x.SetAsync(It.IsAny<HackerNewsStory>(), It.IsAny<string>()), Times.Never);
            httpClient.Verify(x => x.GetHackerNewsStoryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }
        finally
        {
            await sut.StopAsync(CancellationToken.None);
        }
    }

    private static HackerNewsStory CreateStory(int id, int score) =>
        new("user", 0, id, Array.Empty<int>(), score, 123, $"title-{id}", "story", "https://example.com");
}
