namespace ChatBot.Web.Contracts.Embeddings;

public sealed record TestEmbeddingResponse(
    bool Success,
    int ConfiguredDimensions,
    int ActualDimensions,
    IReadOnlyList<float> SampleValues);
