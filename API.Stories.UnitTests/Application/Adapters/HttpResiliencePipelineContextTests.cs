using API.Stories.Application.Adapters;
using API.Stories.Application.Ports;
using Flurl.Http;
using Moq;

namespace API.Stories.UnitTests.Application.Adapters;

public sealed class HttpResiliencePipelineContextTests
{
    [Fact]
    public async Task Pipeline_Retries_WhenPredicateMatches()
    {
        var context = new HttpResiliencePipelineContext<IHackerNewsHttpClient>(
            retries: 2,
            predicate: response => response.StatusCode != 200);

        var failedResponse = new Mock<IFlurlResponse>(MockBehavior.Strict);
        failedResponse.SetupGet(x => x.StatusCode).Returns(500);

        var attempts = 0;

        _ = await context.Pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            return ValueTask.FromResult(failedResponse.Object);
        }, CancellationToken.None);

        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task Pipeline_DoesNotRetry_WhenPredicateDoesNotMatch()
    {
        var context = new HttpResiliencePipelineContext<IHackerNewsHttpClient>(
            retries: 5,
            predicate: response => response.StatusCode != 200);

        var successResponse = new Mock<IFlurlResponse>(MockBehavior.Strict);
        successResponse.SetupGet(x => x.StatusCode).Returns(200);

        var attempts = 0;

        _ = await context.Pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            return ValueTask.FromResult(successResponse.Object);
        }, CancellationToken.None);

        Assert.Equal(1, attempts);
    }
}
