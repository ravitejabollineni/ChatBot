using ChatBot.Api.Domain.Enums;

namespace ChatBot.Api.Domain.ValueObjects;

public sealed record ChatModelMetadata(
    string Provider,
    string Model,
    int ContextLimit,
    ChatModelCapabilities Capabilities);
