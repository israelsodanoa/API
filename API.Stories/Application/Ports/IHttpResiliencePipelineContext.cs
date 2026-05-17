using Flurl.Http;
using Polly;

namespace API.Stories.Application.Ports;

public interface IHttpResiliencePipelineContext
{
    ResiliencePipeline<IFlurlResponse> Pipeline { get; }
}

public interface IHttpResiliencePipelineContext<T> : IHttpResiliencePipelineContext
{ }