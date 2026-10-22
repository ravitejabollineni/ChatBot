using System.ComponentModel.DataAnnotations;

namespace ChatBot.Api.Features.Embeddings.Endpoints.TestEmbedding;

public sealed record TestEmbeddingRequest(
    [property: Required]
    string Text);
