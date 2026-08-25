namespace ChatBot.Api.Domain.Enums;

[Flags]
public enum ChatModelCapabilities
{
    None = 0,
    Chat = 1,
    Streaming = 2,
    ToolCalling = 4,
    StructuredOutput = 8,
    Embeddings = 16
}
