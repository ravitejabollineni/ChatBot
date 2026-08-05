namespace ChatBot.Api.AI.Configuration;

/// <summary>
/// Settings for the Ollama provider. Bound from "AI:Providers:Ollama".
/// </summary>
public sealed class OllamaOptions
{
    public const string SectionName = "AI:Providers:Ollama";

    public string BaseUrl { get; init; } = string.Empty;

    public string ChatModel { get; init; } = string.Empty;

    public string EmbeddingModel { get; init; } = string.Empty;

    /// <summary>
    /// Context window size in tokens, sent to Ollama as <c>num_ctx</c>. Set to <c>0</c> or
    /// omit to let Ollama size the window itself.
    /// </summary>
    /// <remarks>
    /// Defaults to a bounded window rather than deferring to Ollama, because Ollama sizes
    /// the KV cache to the model's full native context when this is unset. For a
    /// long-context model that is enormous relative to the weights — qwen3:4b has a 256K
    /// window, which projects to roughly 18 GiB of host memory against ~0.7 GiB of weights,
    /// so the load is aborted outright on a 16 GiB machine and <c>/api/chat</c> answers an
    /// opaque HTTP 500. Whether that happens otherwise depends on how much RAM is free at
    /// the moment of the first request, which makes the failure intermittent. Pinning the
    /// window keeps model loads predictable; raise it if you need longer conversations and
    /// have the memory to spare (cost scales roughly linearly — about 70 MiB per 1K tokens
    /// for qwen3:4b).
    /// </remarks>
    public int? NumCtx { get; init; } = 8192;

    /// <summary>
    /// Whether the model may spend output tokens on an internal reasoning pass before
    /// answering. Defaults to <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// Only affects reasoning-capable models, which think by default and are slow when they
    /// do: qwen3:4b spent roughly 200 tokens deliberating over how to phrase "hi", and tens
    /// of seconds returning a one-word answer. The reasoning never reaches the client either
    /// — Ollama reports it separately from the answer text, so the latency buys nothing that
    /// is shown to the user. Enable it for tasks where answer quality justifies the wait.
    /// </remarks>
    public bool EnableThinking { get; init; }
}
