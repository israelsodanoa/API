using API.Stories.Application.Adapters.Services;
using API.Stories.Application.Ports;
using API.Stories.Application.Ports.Boundaries;
using API.Stories.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace API.Stories.UnitTests.Application.Adapters.Services;

public sealed class HackerNewsStoryServiceTests
{
    [Fact]
    public async Task GetTokStoriesAsync_ReturnsError_WhenBestStoriesFails()
    {
        var httpClient = new Mock<IHackerNewsHttpClient>(MockBehavior.Strict);
        httpClient.Setup(x => x.GetBestStoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((Result<int[]>)"upstream failure");

        var cache = new Mock<ICacheService>(MockBehavior.Strict);

        var sut = new HackerNewsStoryService(httpClient.Object, cache.Object, NullLogger<HackerNewsStoryService>.Instance);

        var result = await sut.GetTokStoriesAsync(3, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("upstream failure", result.Error);
    }

    [Fact]
    public async Task GetTokStoriesAsync_ReturnsHighestScoresFromCache()
    {
        var ids = new[] { 1, 2, 3, 4 };

        var httpClient = new Mock<IHackerNewsHttpClient>(MockBehavior.Strict);
        httpClient.Setup(x => x.GetBestStoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<int[]>(ids));

        var stories = new Dictionary<string, HackerNewsStory>
        {
            [HackerNewsStory.GetCacheKey(1)] = CreateStory(1, 10),
            [HackerNewsStory.GetCacheKey(2)] = CreateStory(2, 30),
            [HackerNewsStory.GetCacheKey(3)] = CreateStory(3, 20),
            [HackerNewsStory.GetCacheKey(4)] = CreateStory(4, 50)
        };

        var cache = new Mock<ICacheService>(MockBehavior.Strict);
        cache.Setup(x => x.GetAsync<HackerNewsStory>(It.IsAny<string>()))
            .Returns((string key) =>
                Task.FromResult(stories.TryGetValue(key, out var story) ? story : null)!);

        var sut = new HackerNewsStoryService(httpClient.Object, cache.Object, NullLogger<HackerNewsStoryService>.Instance);

        var result = await sut.GetTokStoriesAsync(3, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(new[] { 50, 30 }, result.Data.Select(x => x.Score).ToArray());
    }

    [Fact]
    public async Task GetTokStoriesAsync_IgnoresMissingCacheEntries()
    {
        var ids = new[] { 1, 2, 3 };

        var httpClient = new Mock<IHackerNewsHttpClient>(MockBehavior.Strict);
        httpClient.Setup(x => x.GetBestStoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<int[]>(ids));

        var stories = new Dictionary<string, HackerNewsStory>
        {
            [HackerNewsStory.GetCacheKey(1)] = CreateStory(1, 10),
            [HackerNewsStory.GetCacheKey(3)] = CreateStory(3, 20)
        };

        var cache = new Mock<ICacheService>(MockBehavior.Strict);
        cache.Setup(x => x.GetAsync<HackerNewsStory>(It.IsAny<string>()))
            .Returns((string key) =>
                Task.FromResult(stories.TryGetValue(key, out var story) ? story : null)!);

        var sut = new HackerNewsStoryService(httpClient.Object, cache.Object, NullLogger<HackerNewsStoryService>.Instance);

        var result = await sut.GetTokStoriesAsync(10, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(new[] { 3, 1 }, result.Data.Select(x => x.Id).ToArray());
    }

    private static HackerNewsStory CreateStory(int id, int score) =>
        new("user", 0, id, Array.Empty<int>(), score, 123, $"title-{id}", "story", "https://example.com");
}
