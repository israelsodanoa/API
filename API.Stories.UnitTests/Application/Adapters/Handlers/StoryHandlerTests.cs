using API.Stories.Application.Adapters.Handlers;
using API.Stories.Application.Ports;
using API.Stories.Application.Ports.Boundaries;
using API.Stories.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;

namespace API.Stories.UnitTests.Application.Adapters.Handlers;

public sealed class StoryHandlerTests
{
    [Fact]
    public async Task GetTopkStories_ReturnsBadRequest_WhenServiceFails()
    {
        var service = new Mock<IHackerNewsStoryService>(MockBehavior.Strict);
        service.Setup(x => x.GetTokStoriesAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<HackerNewsStory[]>(default!, "error"));

        var result = await StoryHandler.GetTopkStories(3, service.Object, CancellationToken.None);
        var badRequest = Assert.IsType<BadRequest<string>>(result);

        Assert.Equal("error", badRequest.Value);
    }

    [Fact]
    public async Task GetTopkStories_ReturnsOk_WhenServiceSucceeds()
    {
        var stories = new[]
        {
            new HackerNewsStory("user", 0, 1, Array.Empty<int>(), 12, 123, "title", "story", "https://x")
        };

        var service = new Mock<IHackerNewsStoryService>(MockBehavior.Strict);
        service.Setup(x => x.GetTokStoriesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<HackerNewsStory[]>(stories));

        var result = await StoryHandler.GetTopkStories(1, service.Object, CancellationToken.None);
        var ok = Assert.IsType<Ok<HackerNewsStory[]>>(result);

        Assert.NotNull(ok.Value);
        Assert.Single(ok.Value);
        Assert.Equal(1, ok.Value[0].Id);
    }
}
