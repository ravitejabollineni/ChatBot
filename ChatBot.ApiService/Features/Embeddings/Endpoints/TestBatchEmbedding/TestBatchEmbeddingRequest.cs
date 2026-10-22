using System.ComponentModel.DataAnnotations;

namespace ChatBot.Api.Features.Embeddings.Endpoints.TestBatchEmbedding;

public sealed record TestBatchEmbeddingRequest(
    [property: Required]
    IReadOnlyList<string> Texts);
