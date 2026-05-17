using API.Stories.Application.Ports.Boundaries;
using API.Stories.Domain;

namespace API.Stories.Application.Ports;

public interface IHackerNewsHttpClient
{
    Task<Result<int[]>> GetBestStoriesAsync(
       CancellationToken cancellationToken);

    Task<Result<HackerNewsStory>> GetHackerNewsStoryAsync(
        int id,
        CancellationToken cancellationToken);
}