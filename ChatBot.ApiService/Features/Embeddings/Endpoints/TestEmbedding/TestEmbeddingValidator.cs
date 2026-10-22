using FastEndpoints;
using FluentValidation;

namespace ChatBot.Api.Features.Embeddings.Endpoints.TestEmbedding;

public sealed class TestEmbeddingValidator
    : Validator<TestEmbeddingRequest>
{
    public TestEmbeddingValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty();

        RuleFor(x => x.Text)
            .MaximumLength(4000);
    }
}
