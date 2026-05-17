using API.Stories.Application.Ports;
using Microsoft.AspNetCore.Mvc;

namespace API.Stories.Application.Adapters.Handlers;

public static class StoryHandler
{
    public static async Task<IResult> GetTopkStories(
        int topk,
        [FromServices] IHackerNewsStoryService service,
        CancellationToken cancellationToken)
    {
        var res = await service.GetTokStoriesAsync(topk, cancellationToken);
        if (!res)
            return Results.BadRequest(res.Error);

        return Results.Ok(res.Data);
    }
}