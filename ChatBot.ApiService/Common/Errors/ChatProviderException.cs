namespace ChatBot.Api.Common.Errors
{
    public sealed class ChatProviderException(
        string message,
        string provider,
        string? model = null,
        Exception? innerException = null)
        : Exception(message, innerException)
    {
        public string Provider { get; } = provider;

        public string? Model { get; } = model;
    }
}
