using ChatBot.Web;
using ChatBot.Web.Components;
using ChatBot.Web.Features.Chat.Services;
using ChatBot.Web.Features.Chat.Services.Api;
using ChatBot.Web.Features.Chat.State;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Refit;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOutputCache();

builder.Services
    .AddRefitClient<IConversationApi>()
    .ConfigureHttpClient(c =>
    {
        c.BaseAddress = new Uri("http://api");
    });

// Sending a message triggers a real LLM completion on the API side,
// which can legitimately take longer than the 30s default applied to
// every HttpClient by ConfigureHttpClientDefaults. Replace it with a
// plain timeout (no retry, no circuit breaker): the "standard" bundle's
// own validator requires MaxRetryAttempts >= 1 and CircuitBreaker.
// SamplingDuration >= 2x AttemptTimeout, which fights a deliberately
// long, no-retry single attempt. POST /api/chat/messages isn't
// idempotent, so retrying a request that merely timed out (rather than
// actually failed) risks sending the same message twice.
#pragma warning disable EXTEXP0001
builder.Services
    .AddRefitClient<IChatApi>()
    .ConfigureHttpClient(c =>
    {
        c.BaseAddress = new Uri("http://api");
        // HttpClient's own 100s default would otherwise cut the request
        // off before the 3-minute Polly timeout below gets a chance to.
        c.Timeout = Timeout.InfiniteTimeSpan;
    })
    .RemoveAllResilienceHandlers()
    .AddResilienceHandler("send-message-timeout", pipeline =>
    {
        pipeline.AddTimeout(TimeSpan.FromMinutes(3));
    });
#pragma warning restore EXTEXP0001

builder.Services.AddScoped<ChatState>();

builder.Services.AddScoped<ChatUiService>();
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();


