using ChatBot.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatBot.Api.Infrastructure.Persistence.Configurations;

public sealed class ConversationMessageConfiguration
    : IEntityTypeConfiguration<ConversationMessage>
{
    public void Configure(
        EntityTypeBuilder<ConversationMessage> builder)
    {
        builder.ToTable("conversation_messages");

        builder.HasKey(x => x.Id)
            .HasName("pk_conversation_messages");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.ConversationId)
            .HasColumnName("conversation_id")
            .IsRequired();

        builder.Property(x => x.Role)
             .HasColumnName("role")
             .IsRequired();

        builder.Property(x => x.Content)
            .HasColumnName("content")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.IsPartial)
            .HasColumnName("is_partial")
            .IsRequired();

        builder.ComplexProperty(
            x => x.TokenUsage,
            tokenUsage =>
            {
                tokenUsage.Property(x => x.InputTokenCount)
                    .HasColumnName("input_token_count");

                tokenUsage.Property(x => x.OutputTokenCount)
                    .HasColumnName("output_token_count");

                tokenUsage.Property(x => x.ContextLimit)
                    .HasColumnName("context_limit");

                tokenUsage.Property(x => x.RemainingTokenBudget)
                    .HasColumnName("remaining_token_budget");

                tokenUsage.Property(x => x.PercentageUsed)
                    .HasColumnName("percentage_used");
            });
    }
}