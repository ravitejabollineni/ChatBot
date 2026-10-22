using ChatBot.Api.AI.Configuration;
using FastEndpoints;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace ChatBot.Api.Features.Embeddings.Endpoints.TestEmbedding;

/// <summary>
/// Development-only diagnostic that proves the embedding pipeline end-to-end
/// (<see cref="IEmbeddingGenerator{TInput,TEmbedding}"/> -> the configured Azure OpenAI
/// embedding deployment) without exposing the full vector or any provider secrets. Tagged
/// "dev-only" so Program.cs's endpoint filter excludes it outside Development.
/// </summary>
public sealed class TestEmbeddingEndpoint(
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    IOptions<EmbeddingOptions> embeddingOptions)
    : Endpoint<TestEmbeddingRequest, TestEmbeddingResponse>
{
    private const int SampleValueCount = 5;

    public override void Configure()
    {
        Post("/api/embeddings/test");
        AllowAnonymous();
        Tags("dev-only");
        Summary(summary =>
        {
            summary.Summary = "Test Embedding Generation";
            summary.Description =
                "Generates an embedding for the supplied text and reports its dimensions. Development-only diagnostic.";
        });
    }

    public override async Task HandleAsync(TestEmbeddingRequest request, CancellationToken ct)
    {
        var embedding = await embeddingGenerator.GenerateAsync(request.Text, cancellationToken: ct);

        var vector = embedding.Vector;

        if (vector.Length == 0)
        {
            throw new InvalidOperationException("Embedding generation returned an empty vector.");
        }

        var configuredDimensions = embeddingOptions.Value.Dimensions;

        if (vector.Length != configuredDimensions)
        {
            throw new InvalidOperationException(
                $"Embedding dimension mismatch. Configured dimensions: {configuredDimensions}. Actual dimensions: {vector.Length}.");
        }

        await Send.OkAsync(new TestEmbeddingResponse(
            Success: true,
            ConfiguredDimensions: configuredDimensions,
            ActualDimensions: vector.Length,
            SampleValues: vector.Span[..Math.Min(SampleValueCount, vector.Length)].ToArray()));
    }
}
