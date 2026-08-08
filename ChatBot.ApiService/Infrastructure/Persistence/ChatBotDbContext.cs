using ChatBot.Api.Domain.Entities;
using ChatBot.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ChatBot.Api.Infrastructure.Persistence;

public sealed class ChatBotDbContext : DbContext
{
    public ChatBotDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<ConversationMessage> ConversationMessages =>
        Set<ConversationMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresEnum<ChatRole>(
           name: "chat_role");

        modelBuilder.HasPostgresEnum<ConversationTitleStatus>(
            name: "conversation_title_status");
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ChatBotDbContext).Assembly);
    }

    public string GetModelDebugView()
    {
        return Model.ToDebugString(
            MetadataDebugStringOptions.LongDefault);
    }
}