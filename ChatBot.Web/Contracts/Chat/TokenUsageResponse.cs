namespace ChatBot.Web.Contracts.Chat;

public sealed record TokenUsageResponse(
    int InputTokenCount,
    int OutputTokenCount,
    int ContextLimit,
    int RemainingTokenBudget,
    double PercentageUsed);
