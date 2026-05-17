using API.Stories.Application.Adapters.HttpClients;
using API.Stories.Application.Ports;
using API.Stories.Application.Ports.Boundaries;
using Flurl.Http;
using Flurl.Http.Configuration;
using Flurl.Http.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;

namespace API.Stories.UnitTests.Application.Adapters.HttpClients;

public sealed class HackerNewsHttpClientTests
{
    [Fact]
    public async Task GetBestStoriesAsync_ReturnsParsedIds_WhenResponseIs200()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new[] { 1, 2, 3 }, 200);

        var sut = CreateSut();

        var result = await sut.GetBestStoriesAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(new[] { 1, 2, 3 }, result.Data);
        httpTest.ShouldHaveCalled("https://hn.test/v0/beststories.json").WithVerb(HttpMethod.Get);
    }

    [Fact]
    public async Task GetBestStoriesAsync_ReturnsErrorBody_WhenResponseIsNot200()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith("service unavailable", 503);

        var sut = CreateSut();

        var result = await sut.GetBestStoriesAsync(CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("service unavailable", result.Error);
    }

    [Fact]
    public async Task GetHackerNewsStoryAsync_ReturnsStory_WhenResponseIs200()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new
        {
            by = "user",
            descendants = 0,
            id = 10,
            kids = Array.Empty<int>(),
            score = 55,
            time = 123,
            title = "title",
            type = "story",
            url = "https://example.com"
        }, 200);

        var sut = CreateSut();

        var result = await sut.GetHackerNewsStoryAsync(10, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(10, result.Data.Id);
        Assert.Equal(55, result.Data.Score);
        httpTest.ShouldHaveCalled("https://hn.test/v0/item/10.json").WithVerb(HttpMethod.Get);
    }

    [Fact]
    public async Task GetHackerNewsStoryAsync_ReturnsErrorBody_WhenResponseIsNot200()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith("not found", 404);

        var sut = CreateSut();

        var result = await sut.GetHackerNewsStoryAsync(404, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("not found", result.Error);
    }

    private static HackerNewsHttpClient CreateSut()
    {
        var settings = new HackerNewsHttpClientSettings("https://hn.test", 0, 5000);
        var resilience = new PassThroughResilienceContext<IHackerNewsHttpClient>();
        var cache = new FlurlClientCache();
        return new HackerNewsHttpClient(settings, resilience, cache, NullLogger<HackerNewsHttpClient>.Instance);
    }

    private sealed class PassThroughResilienceContext<T> : IHttpResiliencePipelineContext<T>
    {
        public ResiliencePipeline<IFlurlResponse> Pipeline { get; } = new ResiliencePipelineBuilder<IFlurlResponse>().Build();
    }
}
