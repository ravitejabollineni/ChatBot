using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace ChatBot.Web.Features.Chat.Services.Api;

// The API's GlobalExceptionHandler always returns RFC 7807 ProblemDetails on failure, with a
// Title that is deliberately safe to show as-is (e.g. "Chat provider error", "Conversation not
// found") - never raw exception details. Both chat clients (Refit's IChatApi and the hand-rolled
// SSE ChatStreamClient) need to turn that body into the same user-facing text, so the parsing
// lives here once instead of twice.
internal static class ApiProblemDetailsReader
{
    public static ProblemDetails? TryRead(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ProblemDetails>(body, JsonSerializerOptions.Web);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
