using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace API.Stories.Application.Adapters;

public static class ResiliencePolicies
{
    public static ResiliencePipeline<T> RetryWithCircuitBreaker<T>(in int retries, in Func<T, bool> predicate)
    {
        var predicateBuilder = new PredicateBuilder<T>()
                                    .HandleResult(predicate);

        var pipeline = new ResiliencePipelineBuilder<T>()
        .AddRetry(new RetryStrategyOptions<T>
        {
            ShouldHandle = predicateBuilder,
            MaxRetryAttempts = retries,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        })
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions<T>
        {
            ShouldHandle = predicateBuilder,
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(10),
            MinimumThroughput = 5,
            BreakDuration = TimeSpan.FromSeconds(30)
        })
        .Build();

        return pipeline;
    }
}