using ChatBot.Api.AI.Configuration;
using FastEndpoints;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace ChatBot.Api.Features.Embeddings.Endpoints.TestBatchEmbedding;

/// <summary>
/// Development-only diagnostic that proves batch embedding generation: one collection-input
/// <see cref="IEmbeddingGenerator{TInput,TEmbedding}.GenerateAsync"/> call for all texts, then
/// count/dimension checks and a positional pairing of each text with its vector. Tagged
/// "dev-only" so Program.cs's endpoint filter excludes it outside Development.
/// </summary>
public sealed class TestBatchEmbeddingEndpoint(
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    IOptions<EmbeddingOptions> embeddingOptions)
    : Endpoint<TestBatchEmbeddingRequest, TestBatchEmbeddingResponse>
{
    private const int SampleValueCount = 5;

    public override void Configure()
    {
        Post("/api/embeddings/test-batch");
        AllowAnonymous();
        Tags("dev-only");
        Summary(summary =>
        {
            summary.Summary = "Test Batch Embedding Generation";
            summary.Description =
                "Generates embeddings for several texts in a single call and verifies count, dimensions and ordering. Development-only diagnostic.";
        });
    }

    public override async Task HandleAsync(TestBatchEmbeddingRequest request, CancellationToken ct)
    {
        var texts = request.Texts;

        var embeddings = await embeddingGenerator.GenerateAsync(texts, cancellationToken: ct);

        if (embeddings.Count != texts.Count)
        {
            throw new InvalidOperationException(
                $"Embedding count mismatch. Inputs: {texts.Count}. Embeddings returned: {embeddings.Count}.");
        }

        var configuredDimensions = embeddingOptions.Value.Dimensions;
        var results = new List<BatchEmbeddingResult>(texts.Count);

        // The generator contract returns embeddings in input order, so index i of the output is
        // paired with index i of the input.
        for (var i = 0; i < embeddings.Count; i++)
        {
            var vector = embeddings[i].Vector;

            if (vector.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Embedding generation returned an empty vector for input at index {i}.");
            }

            if (vector.Length != configuredDimensions)
            {
                throw new InvalidOperationException(
                    $"Embedding dimension mismatch at index {i}. Configured dimensions: {configuredDimensions}. Actual dimensions: {vector.Length}.");
            }

            results.Add(new BatchEmbeddingResult(
                Index: i,
                Text: texts[i],
                ActualDimensions: vector.Length,
                SampleValues: vector.Span[..Math.Min(SampleValueCount, vector.Length)].ToArray()));
        }

        await Send.OkAsync(new TestBatchEmbeddingResponse(
            Success: true,
            InputCount: texts.Count,
            OutputCount: embeddings.Count,
            ConfiguredDimensions: configuredDimensions,
            Results: results));
    }
}
