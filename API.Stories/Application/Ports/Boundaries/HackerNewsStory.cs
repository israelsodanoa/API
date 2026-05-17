using System.Text.Json.Serialization;

namespace API.Stories.Application.Ports.Boundaries;

public record HackerNewsStory(
    [property: JsonPropertyName("by")] string By,
    [property: JsonPropertyName("descendants")] int Descendants,
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("kids")] int[] Kids,
    [property: JsonPropertyName("score")] int Score,
    [property: JsonPropertyName("time")] int Time,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("url")] string Url
)
{
    public static string GetCacheKey(int id) => $"story-{id}";
}