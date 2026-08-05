using FastEndpoints;
using FluentValidation;

namespace ChatBot.Api.Features.Chat.Endpoints.StreamMessage;

/// <summary>
/// Mirrors <see cref="SendMessage.SendMessageValidator"/> — the streaming endpoint takes the
/// same input as the non-streaming one, so it enforces the same rules. Validation runs before
/// <c>HandleAsync</c>, which matters more here than elsewhere: once the SSE response has
/// started, the status code is committed and a 400 can no longer be sent.
/// </summary>
public sealed class StreamMessageValidator
    : Validator<StreamMessageRequest>
{
    public StreamMessageValidator()
    {
        RuleFor(x => x.Model)
            .NotEmpty();

        RuleFor(x => x.Message)
            .NotEmpty();

        RuleFor(x => x.Message)
            .MaximumLength(4000);

        RuleFor(x => x.ConversationId)
            .NotEmpty();
    }
}
