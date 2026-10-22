namespace ChatBot.Api.Features.Embeddings.Endpoints.TestEmbedding;

public sealed record TestEmbeddingResponse(
    bool Success,
    int ConfiguredDimensions,
    int ActualDimensions,
    IReadOnlyList<float> SampleValues);
