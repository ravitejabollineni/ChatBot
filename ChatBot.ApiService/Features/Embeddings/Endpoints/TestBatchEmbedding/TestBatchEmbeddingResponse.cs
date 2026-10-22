namespace ChatBot.Api.Features.Embeddings.Endpoints.TestBatchEmbedding;

public sealed record TestBatchEmbeddingResponse(
    bool Success,
    int InputCount,
    int OutputCount,
    int ConfiguredDimensions,
    IReadOnlyList<BatchEmbeddingResult> Results);

public sealed record BatchEmbeddingResult(
    int Index,
    string Text,
    int ActualDimensions,
    IReadOnlyList<float> SampleValues);
