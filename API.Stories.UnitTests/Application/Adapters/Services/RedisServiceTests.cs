using System.Text.Json;
using API.Stories.Application.Adapters.Services;
using API.Stories.Application.Ports.Boundaries;
using Moq;
using StackExchange.Redis;

namespace API.Stories.UnitTests.Application.Adapters.Services;

public sealed class RedisServiceTests
{
    [Fact]
    public async Task GetAsync_ReturnsDeserializedValue_WhenCacheContainsValidJson()
    {
        var expected = CreateStory(1, 12);
        var serialized = JsonSerializer.Serialize(expected);

        var db = new Mock<IDatabase>(MockBehavior.Strict);
        db.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(serialized);

        var connection = new Mock<IConnectionMultiplexer>(MockBehavior.Strict);
        connection.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(db.Object);

        var sut = new RedisService(connection.Object);

        var result = await sut.GetAsync<HackerNewsStory>("story-1");

        Assert.NotNull(result);
        Assert.Equal(expected.Id, result.Id);
        Assert.Equal(expected.Score, result.Score);
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenDatabaseThrows()
    {
        var db = new Mock<IDatabase>(MockBehavior.Strict);
        db.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new Exception("redis unavailable"));

        var connection = new Mock<IConnectionMultiplexer>(MockBehavior.Strict);
        connection.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(db.Object);

        var sut = new RedisService(connection.Object);

        var result = await sut.GetAsync<HackerNewsStory>("story-1");

        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_WritesSerializedValueAndReturnsInput()
    {
        var story = CreateStory(7, 99);

        RedisValue captured = RedisValue.Null;
        var db = new Mock<IDatabase>(MockBehavior.Strict);
        db.Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<Expiration>(),
                It.IsAny<ValueCondition>(),
                It.IsAny<CommandFlags>()))
            .Callback<RedisKey, RedisValue, Expiration, ValueCondition, CommandFlags>((_, value, _, _, _) => captured = value)
            .ReturnsAsync(true);

        var connection = new Mock<IConnectionMultiplexer>(MockBehavior.Strict);
        connection.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(db.Object);

        var sut = new RedisService(connection.Object);

        var result = await sut.SetAsync(story, HackerNewsStory.GetCacheKey(story.Id));

        Assert.Equal(story, result);

        var parsed = JsonSerializer.Deserialize<HackerNewsStory>(captured.ToString());
        Assert.NotNull(parsed);
        Assert.Equal(story.Id, parsed.Id);
        Assert.Equal(story.Score, parsed.Score);
    }

    private static HackerNewsStory CreateStory(int id, int score) =>
        new("user", 0, id, Array.Empty<int>(), score, 123, $"title-{id}", "story", "https://example.com");
}
