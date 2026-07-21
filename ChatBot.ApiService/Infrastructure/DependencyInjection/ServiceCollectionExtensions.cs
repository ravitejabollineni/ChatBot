using Azure;
using Azure.AI.OpenAI;
using ChatBot.Api.Features.Chat.Contracts;
using ChatBot.Api.Features.Chat.Services;
using ChatBot.Api.Features.Conversations;
using ChatBot.Api.Features.Conversations.Contracts;
using ChatBot.Api.Infrastructure.Configuration;
using ChatBot.Api.Infrastructure.Persistence;
using ChatBot.Api.Infrastructure.Providers.AzureOpenAI;
using ChatBot.Api.Infrastructure.Providers.Factory;
using ChatBot.Api.Infrastructure.Providers.OpenAI;
using ChatBot.Api.Infrastructure.TokenManagement;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using Polly;
using System.ClientModel.Primitives;

namespace ChatBot.Api.Infrastructure.DependencyInjection;

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

        services.Configure<OpenAiOptions>(configuration.GetSection(OpenAiOptions.SectionName));

        services.Configure<AzureOpenAiOptions>(configuration.GetSection(AzureOpenAiOptions.SectionName));

        services.Configure<SystemPromptOptions>(configuration.GetSection(SystemPromptOptions.SectionName));

        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<OpenAiOptions>>()
                .Value;

            return new OpenAIClient(options.ApiKey);
        });

        services.AddSingleton<IChatProvider, AzureOpenAiChatProvider>();
        services.AddSingleton<IChatProvider, OpenAiChatProvider>();

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
#pragma warning restore EXTEXP0001

        services.AddChatClient(sp =>
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
        return services;
    }
}