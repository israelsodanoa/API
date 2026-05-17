using API.Stories.Application.Ports;
using Flurl.Http;
using Polly;

namespace API.Stories.Application.Adapters;

public sealed class HttpResiliencePipelineContext<T>(
    int retries,
    Func<IFlurlResponse, bool> predicate) : IHttpResiliencePipelineContext<T>
{

    private readonly ResiliencePipeline<IFlurlResponse> __pipe =
        ResiliencePolicies.RetryWithCircuitBreaker(retries, predicate);

    public ResiliencePipeline<IFlurlResponse> Pipeline => __pipe;
}

