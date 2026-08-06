using Azure;
using Azure.AI.OpenAI;
using ChatBot.Api.AI.Configuration;
using ChatBot.Api.AI.Prompting;
using ChatBot.Api.AI.Prompting.Contracts;
using ChatBot.Api.AI.Prompts.Contracts;
using ChatBot.Api.AI.Prompts.Parsing;
using ChatBot.Api.AI.Prompts.Repository;
using ChatBot.Api.AI.Providers.AzureOpenAI;
using ChatBot.Api.AI.Providers.Gemini;
using ChatBot.Api.AI.Providers.Ollama;
using ChatBot.Api.AI.Providers.OpenAI;
using ChatBot.Api.AI.Routing;
using ChatBot.Api.AI.TokenManagement;
using ChatBot.Api.Features.Chat.Contracts;
using ChatBot.Api.Features.Chat.Services;
using ChatBot.Api.Features.Conversations;
using ChatBot.Api.Features.Conversations.Contracts;
using ChatBot.Api.Infrastructure.Persistence;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OpenAI;
using Polly;
using System.ClientModel.Primitives;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ChatBot.Api.AI.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConversationRepository, InMemoryConversationRepository>();

        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IConversationService, ConversationService>();

        services.AddSingleton<IChatProviderFactory, ChatProviderFactory>();

        services.AddScoped<IChatService, ChatService>();

        services.AddSingleton<ITokenManager, EstimatingTokenManager>();
        services.AddOptions<AiOptions>()
            .Bind(configuration.GetSection(AiOptions.SectionName))
            .Validate(
                o => ChatProviderNames.Normalize(o.DefaultProvider) is not null,
                $"AI:DefaultProvider must be one of: {string.Join(", ", ChatProviderNames.All)}.")
            .Validate(
                o => o.AvailableModels.All(
                    m => string.IsNullOrWhiteSpace(m.Provider) || ChatProviderNames.Normalize(m.Provider) is not null),
                $"AI:AvailableModels[].Provider, when set, must be one of: {string.Join(", ", ChatProviderNames.All)}.")
            .Validate(
                o => !IsSelected(o, ChatProviderNames.Ollama)
                     || (!string.IsNullOrWhiteSpace(o.Providers.Ollama.BaseUrl)
                         && !string.IsNullOrWhiteSpace(o.Providers.Ollama.ChatModel)),
                "AI:Providers:Ollama:BaseUrl and ChatModel are required when AI:DefaultProvider is 'Ollama'.")
            .Validate(
                o => !IsSelected(o, ChatProviderNames.AzureOpenAI)
                     || (Uri.TryCreate(o.Providers.AzureOpenAI.Endpoint, UriKind.Absolute, out _)
                         && !string.IsNullOrWhiteSpace(o.Providers.AzureOpenAI.ApiKey)
                         && !string.IsNullOrWhiteSpace(o.Providers.AzureOpenAI.DeploymentName)),
                "AI:Providers:AzureOpenAI:Endpoint (an absolute URI), ApiKey and DeploymentName "
                + "are required when AI:DefaultProvider is 'AzureOpenAI'.")
            .Validate(
                o => !IsSelected(o, ChatProviderNames.OpenAI)
                     || (!string.IsNullOrWhiteSpace(o.Providers.OpenAI.ApiKey)
                         && !string.IsNullOrWhiteSpace(o.Providers.OpenAI.Model)),
                "AI:Providers:OpenAI:ApiKey and Model are required when AI:DefaultProvider is 'OpenAI'.")
            .Validate(
                o => !IsSelected(o, ChatProviderNames.GeminiAI)
                     || (!string.IsNullOrWhiteSpace(o.Providers.GeminiAi.ApiKey)
                         && !string.IsNullOrWhiteSpace(o.Providers.GeminiAi.ChatModel)),
                "AI:Providers:GeminiAI:ApiKey and ChatModel are required when AI:DefaultProvider is 'GeminiAI'.")
            .ValidateOnStart();


        services.AddOptions<OpenAiOptions>()
            .Bind(configuration.GetSection(OpenAiOptions.SectionName));

        services.AddOptions<AzureOpenAiOptions>()
            .Bind(configuration.GetSection(AzureOpenAiOptions.SectionName));

        services.AddOptions<OllamaOptions>()
            .Bind(configuration.GetSection(OllamaOptions.SectionName));

        services.AddOptions<GeminiOptions>()
            .Bind(configuration.GetSection(GeminiOptions.SectionName));

        services.AddSingleton<IDeserializer>(_ =>
             new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build());

        services.AddKeyedSingleton<IChatProvider, AzureOpenAiChatProvider>(ChatProviderNames.AzureOpenAI);
        services.AddKeyedSingleton<IChatProvider, OpenAiChatProvider>(ChatProviderNames.OpenAI);
        services.AddKeyedSingleton<IChatProvider, OllamaChatProvider>(ChatProviderNames.Ollama);
        services.AddKeyedSingleton<IChatProvider, GeminiChatProvider>(ChatProviderNames.GeminiAI);

        services.AddSingleton<IPromptRepository, FilePromptRepository>();
        services.AddSingleton<IPromptParser, MarkdownPromptParser>();
        services.AddScoped<IConversationBuilder, ConversationBuilder>();
#pragma warning disable EXTEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        services.AddHttpClient("AzureOpenAI")
            .ConfigureHttpClient(c =>
            {
                c.Timeout = Timeout.InfiniteTimeSpan;
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            })
            .RemoveAllResilienceHandlers()
            .AddResilienceHandler("azure-openai-timeout", builder =>
            {
                builder.AddTimeout(TimeSpan.FromMinutes(2));
            });

        // Local Ollama inference can be much slower than a cloud API (cold model loads,
        // CPU-only hosts), so it gets its own generous timeout rather than reusing
        // Azure's 2-minute one.
        services.AddHttpClient("Ollama", (sp, client) =>
            {
                var options = sp
                    .GetRequiredService<IOptions<OllamaOptions>>()
                    .Value;

                if (!string.IsNullOrWhiteSpace(options.BaseUrl))
                {
                    client.BaseAddress = new Uri(options.BaseUrl);
                }

                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            })
            .RemoveAllResilienceHandlers()
            .AddResilienceHandler("ollama-timeout", builder =>
            {
                builder.AddTimeout(TimeSpan.FromMinutes(5));
            });

        services.AddHttpClient("GeminiAI")
            .ConfigureHttpClient(client =>
            {
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            })
            .RemoveAllResilienceHandlers()
            .AddResilienceHandler("gemini-timeout", builder =>
            {
                builder.AddTimeout(TimeSpan.FromMinutes(2));
            });
#pragma warning restore EXTEXP0001

        // Both IChatClient registrations are keyed by provider name. An unkeyed pair would
        // have the second silently replace the first in the container; keying them lets the
        // two coexist and makes each provider's dependency explicit at its call site.
        services.AddKeyedSingleton<IChatClient>(
            ChatProviderNames.Ollama,
            (sp, _) =>
            {
                var options = sp
                    .GetRequiredService<IOptions<OllamaOptions>>()
                    .Value;

                var httpClient = sp
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient("Ollama");

                return new OllamaApiClient(httpClient, options.ChatModel);
            });

        services.AddKeyedSingleton<IChatClient>(
            ChatProviderNames.AzureOpenAI,
            (sp, _) =>
            {
                var options = sp
                    .GetRequiredService<IOptions<AzureOpenAiOptions>>()
                    .Value;

                var httpClient = sp
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient("AzureOpenAI");

                var azureClientOptions = new AzureOpenAIClientOptions
                {
                    Transport = new HttpClientPipelineTransport(httpClient)
                };

                var azureClient = new AzureOpenAIClient(
                    new Uri(options.Endpoint),
                    new AzureKeyCredential(options.ApiKey),
                    azureClientOptions);

                return azureClient
                    .GetChatClient(options.DeploymentName)
                    .AsIChatClient();
            });

        services.AddSingleton<Client>(sp =>
        {
            var options = sp
                .GetRequiredService<IOptions<GeminiOptions>>()
                .Value;

            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();

            var clientOptions = new ClientOptions
            {
                HttpClientFactory = () =>
                    httpClientFactory.CreateClient("GeminiAI")
            };

            return new Client(
                apiKey: options.ApiKey,
                clientOptions: clientOptions);
        });

        return services;
    }

    // "Selected" now means anything ChatProviderFactory could actually resolve a request
    // to: AI:DefaultProvider (the fallback for entries with no explicit Provider, and for
    // an unlisted model string) plus every provider an AvailableModels entry names
    // explicitly. Each has to be configured, or GetProvider fails at request time instead
    // of here at startup.
    private static bool IsSelected(AiOptions options, string providerName)
        => string.Equals(
               ChatProviderNames.Normalize(options.DefaultProvider),
               providerName,
               StringComparison.Ordinal)
           || options.AvailableModels.Any(m =>
               string.Equals(
                   ChatProviderNames.Normalize(m.Provider),
                   providerName,
                   StringComparison.Ordinal));
}