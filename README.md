# ChatBot

A small chat application built on **.NET Aspire**. A Blazor Server front end talks to a backend API, which forwards chat requests to an AI model — either a **local Ollama** model or a cloud provider (Azure OpenAI, OpenAI) — and streams or returns the response back to the UI.

The API is provider-agnostic: chat requests are routed through an `IChatProvider` abstraction, so which model serves a given request is a **configuration change, not a code change**. See [AI providers & models](#ai-providers--models) below, including how to point the app at a local Ollama server.

## Projects

| Project | Description |
|---|---|
| `ChatBot.AppHost` | .NET Aspire orchestrator — starts and wires up the API and Web app together, with the Aspire dashboard for logs/traces. |
| `ChatBot.ApiService` | Backend API (FastEndpoints) that manages conversations and routes chat requests to whichever `IChatProvider` (Ollama, Azure OpenAI, OpenAI) is configured to serve the requested model. |
| `ChatBot.Web` | Blazor Server front end (`InteractiveServer` render mode) that calls the API via Refit, including a server-sent-events streaming client for token-by-token replies. |
| `ChatBot.ServiceDefaults` | Shared Aspire service defaults: OpenTelemetry, health checks, service discovery, HTTP resilience. |

## Technologies

- .NET 10 / C# (`net10.0`)
- .NET Aspire (`Aspire.AppHost.Sdk`)
- ASP.NET Core, Blazor Server (`InteractiveServer` render mode)
- FastEndpoints 8.2.0 (+ `FastEndpoints.AspVersioning`)
- Refit 13.1.0 (typed HTTP clients, Web → API)
- Microsoft.Extensions.AI 10.8.3 — the shared `IChatClient` abstraction used by the Ollama and Azure OpenAI providers
- **OllamaSharp 5.4.30** — talks to a local Ollama server through `Microsoft.Extensions.AI`'s `IChatClient`
- Azure.AI.OpenAI 2.1.0, Azure.Identity 1.21.0 (Azure OpenAI provider, also via `IChatClient`)
- OpenAI 2.12.0 (OpenAI provider — talks to the SDK's `ChatClient` directly, bypassing `Microsoft.Extensions.AI`; see [Provider implementation patterns](#provider-implementation-patterns))
- Microsoft.Extensions.Http.Resilience (Polly-based per-provider timeouts)
- Microsoft.Extensions.ServiceDiscovery
- OpenTelemetry (ASP.NET Core, HTTP client, runtime instrumentation)
- Scalar.AspNetCore (OpenAPI UI for the API)

## Architecture

The backend is layered by responsibility:

- **Endpoints** (`Features/Chat/Endpoints/*`, FastEndpoints) — `SendMessageEndpoint` (`POST /api/chat/messages`, full response), `StreamMessageEndpoint` (`POST /api/chat/messages/stream`, server-sent events), `GetModelsEndpoint` (`GET /api/chat/models`, drives the model picker), plus the `Conversations` endpoints. HTTP in/out only, no business logic.
- **Application services** (`ChatService`, `ConversationService`) — orchestrate a use case; `ChatService` also calculates and persists token usage per turn via `ITokenManager`.
- **Abstractions** (`IChatProviderFactory` / `IChatProvider`, `IConversationService`, `ITokenManager`) — the only things application services depend on.
- **AI module** (`AI/`) — everything provider-specific: `AI/Configuration` (options for each provider), `AI/Providers/{Ollama,AzureOpenAI,OpenAI}` (the `IChatProvider` implementations), `AI/Routing` (`ChatProviderFactory`, `ChatProviderNames`), `AI/DependencyInjection` (`ServiceCollectionExtensions.AddInfrastructure`, where every provider, `IChatClient`, and `HttpClient` is registered).

`ChatService` depends only on `IChatProviderFactory` — it has no knowledge of Ollama, Azure OpenAI, or OpenAI. `ChatProviderFactory` resolves a provider per-request by looking up the requested model in `AI:AvailableModels`, falling back to `AI:DefaultProvider`.

## AI providers & models

All AI configuration lives under the `AI` section of `ChatBot.ApiService/appsettings.json`:

```json
"AI": {
  "DefaultProvider": "Ollama",
  "AvailableModels": [
    { "Model": "phi3:mini",     "Provider": "Ollama" },
    { "Model": "gpt-5.4-mini",  "Provider": "AzureOpenAI" }
  ],
  "Providers": {
    "Ollama": {
      "BaseUrl": "http://localhost:11434",
      "ChatModel": "phi3:mini",
      "EmbeddingModel": "nomic-embed-text",
      "NumCtx": 8192
    },
    "AzureOpenAI": {
      "Endpoint": "default",
      "ApiKey": "default",
      "DeploymentName": "gpt-5.4-mini"
    },
    "OpenAI": {
      "ApiKey": "default",
      "Model": "default"
    }
  }
}
```

- **`AI:DefaultProvider`** — one of `Ollama`, `AzureOpenAI`, `OpenAI`. Serves any model not explicitly listed in `AvailableModels`, and any request for a model with no `Provider` set.
- **`AI:AvailableModels`** — the model picker shown in the chat UI (`GET /api/chat/models`). Each entry is a model name plus an optional `Provider`; when `Provider` is omitted, `DefaultProvider` serves it.
- **`AI:Providers:*`** — one section per provider, each bound to its own options class. Only the provider(s) actually reachable through `DefaultProvider`/`AvailableModels` are validated (and constructed) at startup — an unconfigured provider that nothing routes to is simply never built, so e.g. leaving `AzureOpenAI` blank while using only Ollama is fine.

Adding another model just means adding an entry to `AvailableModels` (and filling in that provider's section if it isn't configured yet) — no code changes required. Adding a brand-new provider means implementing `IChatProvider` under `AI/Providers/<Name>` and registering it in `ServiceCollectionExtensions.AddInfrastructure`, following the existing Ollama/Azure OpenAI/OpenAI pattern.

### Provider implementation patterns

Not all three providers talk to their backend the same way — this is deliberate, to show both approaches side by side:

| Provider | Underlying client | How it's called |
|---|---|---|
| `OllamaChatProvider` | `Microsoft.Extensions.AI`'s `IChatClient` (implemented by OllamaSharp's `OllamaApiClient`) | Vendor-agnostic abstraction |
| `AzureOpenAiChatProvider` | `Microsoft.Extensions.AI`'s `IChatClient` (via `AzureOpenAIClient.GetChatClient(...).AsIChatClient()`) | Vendor-agnostic abstraction |
| `OpenAiChatProvider` | The `OpenAI` SDK's own `OpenAI.Chat.ChatClient`, constructed directly | Vendor-specific SDK, no `Microsoft.Extensions.AI` in between |

Ollama and Azure OpenAI both go through `IChatClient`, so they're built once, injected via keyed DI (`AI/DependencyInjection/ServiceCollectionExtensions.cs`), and their providers call the same `GetResponseAsync`/`GetStreamingResponseAsync` methods regardless of vendor.

The OpenAI provider is intentionally different: it new's up `OpenAI.Chat.ChatClient` itself and calls `CompleteChatAsync`/`CompleteChatStreamingAsync` straight from the OpenAI SDK, with no `IChatClient` in between. It exists to show what wiring a provider directly against its own SDK looks like — useful as a template if you need to add a vendor whose SDK doesn't implement `IChatClient`, or want more direct control over vendor-specific request options than the abstraction exposes.

### Running a local model with Ollama

The app defaults to a local **Ollama** server (`AI:DefaultProvider: "Ollama"`), so it can run entirely offline with no cloud credentials.

1. **Install Ollama** — [ollama.com/download](https://ollama.com/download).
2. **Pull a chat model** that matches (or replaces) `AI:Providers:Ollama:ChatModel`:
   ```bash
   ollama pull phi3:mini
   ```
   Ollama listens on `http://localhost:11434` by default — this must match `AI:Providers:Ollama:BaseUrl`.
3. **Point the app at it** in `ChatBot.ApiService/appsettings.json` (or override locally, see below):
   ```json
   "AI": {
     "DefaultProvider": "Ollama",
     "AvailableModels": [ { "Model": "phi3:mini", "Provider": "Ollama" } ],
     "Providers": {
       "Ollama": {
         "BaseUrl": "http://localhost:11434",
         "ChatModel": "phi3:mini",
         "NumCtx": 8192
       }
     }
   }
   ```
   `BaseUrl` and `ChatModel` are required whenever Ollama is the (or a) selected provider — the app fails fast at startup if they're missing.
4. **Run the app** as usual (see [Running the app](#running-the-app)) — no `dotnet user-secrets` needed for Ollama, since nothing here is a real credential.

To override without editing `appsettings.json` (e.g. per machine), use environment variables:

```bash
ChatProviders__Ollama__ChatModel=qwen3:4b   # (or set AI__Providers__Ollama__ChatModel)
```

or the equivalent `dotnet user-secrets set "AI:Providers:Ollama:ChatModel" "qwen3:4b"` from `ChatBot.ApiService`.

Two extra Ollama-only settings worth knowing about:

- **`NumCtx`** (default `8192`) — the context window size, sent to Ollama as `num_ctx`. Pinned rather than left to Ollama's default because Ollama otherwise sizes the KV cache to the model's *full native* context window — for a long-context model this can be tens of GB of RAM and cause the model load to fail outright (an opaque HTTP 500) on a typical dev machine. Raise it if you need longer conversations and have the memory to spare (roughly 70 MiB per 1K tokens for a model like `qwen3:4b`); set it to `0` to defer to Ollama's default.
- **`EnableThinking`** (default `false`) — whether reasoning-capable models (e.g. `qwen3`) may spend output tokens "thinking" before answering. Off by default: the reasoning pass is slow and never reaches the client anyway (Ollama returns it separately from the answer text).

Local inference is often much slower than a cloud API (cold model loads, CPU-only hosts), so the Ollama `HttpClient` is configured with an infinite timeout / 5-minute resilience timeout rather than the shorter one used for Azure OpenAI.

### Cloud providers (Azure OpenAI / OpenAI)

Fill in the matching `AI:Providers:AzureOpenAI` / `AI:Providers:OpenAI` section and reference the model in `AvailableModels` (or set it as `DefaultProvider`). Don't put real credentials in `appsettings.json` — use `dotnet user-secrets` instead:

```bash
cd ChatBot.ApiService
dotnet user-secrets init
dotnet user-secrets set "AI:Providers:AzureOpenAI:Endpoint" "https://<your-resource>.openai.azure.com/"
dotnet user-secrets set "AI:Providers:AzureOpenAI:ApiKey" "<your-api-key>"
dotnet user-secrets set "AI:Providers:AzureOpenAI:DeploymentName" "<your-deployment-name>"
```

(Alternatively, set the equivalent `AI__Providers__AzureOpenAI__*` environment variables.)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- For the default configuration: [Ollama](https://ollama.com/download) running locally with the configured chat model pulled (see above)
- Optional, if you switch a model to a cloud provider: an **Azure OpenAI** resource (endpoint, API key, deployment name) and/or an **OpenAI** API key

## Running the app

From the repo root:

```bash
dotnet run --project ChatBot.AppHost
```

This starts the API, the Web app, and the Aspire dashboard. Open the dashboard URL printed in the console to see both resources, their logs, and traces, and to get the link to the running Web app.
