namespace ChatBot.Web.Contracts.Embeddings;

public sealed record TestBatchEmbeddingRequest(IReadOnlyList<string> Texts);
