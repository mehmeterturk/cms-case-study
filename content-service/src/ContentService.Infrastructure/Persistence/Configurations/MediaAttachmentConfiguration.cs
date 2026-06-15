using ContentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentService.Infrastructure.Persistence.Configurations;

public class MediaAttachmentConfiguration : IEntityTypeConfiguration<MediaAttachment>
{
    public void Configure(EntityTypeBuilder<MediaAttachment> builder)
    {
        builder.ToTable("media_attachments");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.FileName)
            .IsRequired()
            .HasMaxLength(260);

        builder.Property(m => m.ContentType)
            .IsRequired()
            .HasMaxLength(127);

        builder.Property(m => m.StorageKey)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(m => m.SizeBytes).IsRequired();

        builder.HasIndex(m => m.ContentId);

        // İçerik silinince medya kayıtları da silinir (DB cascade); dosyalar
        // uygulama katmanında temizlenir. Navigation üzerinden Include edilebilir.
        builder.HasOne<Content>()
            .WithMany(c => c.MediaAttachments)
            .HasForeignKey(m => m.ContentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
