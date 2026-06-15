using ContentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentService.Infrastructure.Persistence.Configurations;

public class ContentConfiguration : IEntityTypeConfiguration<Content>
{
    public void Configure(EntityTypeBuilder<Content> builder)
    {
        builder.ToTable("contents");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Body)
            .IsRequired();

        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(250);

        builder.HasIndex(c => c.Slug).IsUnique();

        builder.Property(c => c.UserId)
            .IsRequired();

        builder.HasIndex(c => c.UserId);

        // Enum'u okunabilirlik için metin olarak sakla.
        builder.Property(c => c.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(c => c.CreatedAt).IsRequired();
    }
}
