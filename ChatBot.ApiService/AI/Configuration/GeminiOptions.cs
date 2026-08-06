namespace ChatBot.Api.AI.Configuration
{
    public class GeminiOptions
    {
        public const string SectionName = "AI:Providers:Gemini";

        public string ApiKey { get; init; } = string.Empty;

        public string ChatModel { get; init; } = string.Empty;
    }
}
