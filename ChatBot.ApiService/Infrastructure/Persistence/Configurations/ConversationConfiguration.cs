using ChatBot.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatBot.Api.Infrastructure.Persistence.Configurations;

public sealed class ConversationConfiguration
    : IEntityTypeConfiguration<Conversation>
{
    public void Configure(
        EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations");

        builder.HasKey(x => x.Id)
            .HasName("pk_conversations");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.LastUpdatedAt)
            .HasColumnName("last_updated_at")
            .IsRequired();

        builder.ComplexProperty(
            x => x.Metadata,
            metadata =>
            {
                metadata.Property(x => x.Title)
                    .HasColumnName("title")
                    .HasMaxLength(200)
                    .IsRequired();

                metadata.Property(x => x.TitleStatus)
            .HasColumnName("title_status")
            .IsRequired();
                metadata.Property(x => x.Preview)
                    .HasColumnName("preview")
                    .HasMaxLength(140);
            });

        builder.HasMany(x => x.Messages)
    .WithOne()
    .HasForeignKey(x => x.ConversationId)
    .HasConstraintName(
        "fk_conversation_messages_conversation")
    .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Messages)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}