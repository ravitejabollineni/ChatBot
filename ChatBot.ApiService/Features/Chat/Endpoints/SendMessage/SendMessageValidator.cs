using FastEndpoints;
using FluentValidation;

namespace ChatBot.Api.Features.Chat.Endpoints.SendMessage;

public sealed class SendMessageValidator
    : Validator<SendMessageRequest>
{
    public SendMessageValidator()
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