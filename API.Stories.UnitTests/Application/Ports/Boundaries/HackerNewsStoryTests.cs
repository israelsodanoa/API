using API.Stories.Application.Ports.Boundaries;

namespace API.Stories.UnitTests.Application.Ports.Boundaries;

public sealed class HackerNewsStoryTests
{
    [Fact]
    public void GetCacheKey_ReturnsExpectedPattern()
    {
        var key = HackerNewsStory.GetCacheKey(123);

        Assert.Equal("story-123", key);
    }
}
