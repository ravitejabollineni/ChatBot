using ChatBot.Api.AI.DependencyInjection;
using ChatBot.Api.Common.Errors;
using ChatBot.Api.Domain.Enums;
using ChatBot.Api.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();

builder.Services.AddFastEndpoints();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddDbContext<ChatBotDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("chatbot"),
        npgsqlOptions =>
        {
            npgsqlOptions.MapEnum<ChatRole>();
            npgsqlOptions.MapEnum<ConversationTitleStatus>();
        });
});
var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.MapScalarApiReference();
app.UseFastEndpoints();
app.MapDefaultEndpoints();

app.Run();

