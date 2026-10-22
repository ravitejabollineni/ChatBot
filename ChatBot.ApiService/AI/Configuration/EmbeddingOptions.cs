namespace ChatBot.Api.AI.Configuration
{
    public sealed class EmbeddingOptions
    {
        public const string SectionName = "AI:Embeddings";
        public string Provider { get; init; } = string.Empty;

        public string Model { get; init; } = string.Empty;

        public int Dimensions { get; init; }
    }
}
