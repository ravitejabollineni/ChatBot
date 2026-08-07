using ChatBot.Api.Domain.Entities;
using ChatBot.Api.Domain.Enums;

namespace ChatBot.Api.Domain.ValueObjects;

public sealed record ConversationMetadata(
    string Title,
    ConversationTitleStatus TitleStatus,
    string? Preview)
{
    public static ConversationMetadata CreateDefault() =>
        new(ConversationDefaults.UntitledTitle, ConversationTitleStatus.NotGenerated, null);
}
