using API.Stories.Application.Ports.Boundaries;
using API.Stories.Domain;

namespace API.Stories.Application.Ports;

public interface IHackerNewsStoryService
{
    Task<Result<HackerNewsStory[]>> GetTokStoriesAsync(
        int topk,
        CancellationToken cancellationToken);
}