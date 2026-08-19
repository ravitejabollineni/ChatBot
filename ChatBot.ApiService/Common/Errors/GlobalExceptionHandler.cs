using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatBot.Api.Common.Errors;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is ChatProviderException chatProviderException)
        {
            logger.LogError(
                chatProviderException,
                "Chat provider failure. Provider: {Provider}, Model: {Model}, TraceId: {TraceId}",
                chatProviderException.Provider,
                chatProviderException.Model,
                httpContext.TraceIdentifier);
        }
        else
        {
            logger.LogError(
                exception,
                "Unhandled exception. TraceId: {TraceId}",
                httpContext.TraceIdentifier);
        }

        var (statusCode, title) = exception switch
        {
            ConversationNotFoundException =>
                 (StatusCodes.Status404NotFound, "Conversation not found"),

            ConversationContextTooLargeException =>
                 (StatusCodes.Status400BadRequest, "Conversation context too large"),

            ChatProviderException =>
                 (StatusCodes.Status502BadGateway, "Chat provider error"),

            KeyNotFoundException =>
                (StatusCodes.Status404NotFound, "Resource not found"),

            ArgumentException =>
                (StatusCodes.Status400BadRequest, "Invalid request"),

            InvalidOperationException =>
                (StatusCodes.Status400BadRequest, "Invalid operation"),

            _ =>
                (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = $"https://httpstatuses.com/{statusCode}",
            Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
        };

        if (environment.IsDevelopment())
        {
            problemDetails.Detail = exception.Message;
        }

        problemDetails.Extensions["traceId"] =
            httpContext.TraceIdentifier;

        problemDetails.Extensions["timestamp"] =
            DateTimeOffset.UtcNow;

        await problemDetailsService.WriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problemDetails
            });

        return true;
    }
}