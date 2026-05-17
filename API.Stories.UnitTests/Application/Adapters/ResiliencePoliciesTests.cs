using API.Stories.Application.Adapters;
using Polly.CircuitBreaker;

namespace API.Stories.UnitTests.Application.Adapters;

public sealed class ResiliencePoliciesTests
{
    [Fact]
    public async Task RetryWithCircuitBreaker_RetriesUntilResultIsHealthy()
    {
        var pipeline = ResiliencePolicies.RetryWithCircuitBreaker<int>(
            retries: 2,
            predicate: value => value < 0);

        var attempts = 0;

        var result = await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            return ValueTask.FromResult(attempts < 3 ? -1 : 42);
        }, CancellationToken.None);

        Assert.Equal(3, attempts);
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task RetryWithCircuitBreaker_OpensCircuitAfterSustainedFailures()
    {
        var pipeline = ResiliencePolicies.RetryWithCircuitBreaker<int>(
            retries: 1,
            predicate: value => value < 0);

        var opened = false;

        for (var i = 0; i < 20; i++)
        {
            try
            {
                _ = await pipeline.ExecuteAsync(
                    _ => ValueTask.FromResult(-1),
                    CancellationToken.None);
            }
            catch (BrokenCircuitException)
            {
                opened = true;
                break;
            }
        }

        Assert.True(opened);
    }
}
