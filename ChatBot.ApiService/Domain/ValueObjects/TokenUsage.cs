namespace ChatBot.Api.Domain.ValueObjects;

public sealed record TokenUsage(
    int InputTokenCount,
    int OutputTokenCount,
    int ContextLimit,
    int RemainingTokenBudget,
    double PercentageUsed);
