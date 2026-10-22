using FastEndpoints;
using FluentValidation;

namespace ChatBot.Api.Features.Embeddings.Endpoints.TestBatchEmbedding;

public sealed class TestBatchEmbeddingValidator
    : Validator<TestBatchEmbeddingRequest>
{
    public const int MinInputs = 2;
    public const int MaxInputs = 10;
    public const int MaxTextLength = 4000;

    public TestBatchEmbeddingValidator()
    {
        RuleFor(x => x.Texts)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(texts => texts.Count >= MinInputs)
                .WithMessage($"At least {MinInputs} texts are required.")
            .Must(texts => texts.Count <= MaxInputs)
                .WithMessage($"At most {MaxInputs} texts are allowed.");

        RuleForEach(x => x.Texts)
            .NotEmpty()
            .MaximumLength(MaxTextLength);
    }
}
