using ChatBot.Web.Contracts.Embeddings;
using Refit;

namespace ChatBot.Web.Services.Api;

public interface IEmbeddingApi
{
    [Post("/api/embeddings/test")]
    Task<TestEmbeddingResponse> TestEmbeddingAsync([Body] TestEmbeddingRequest request);
}
