using ContentService.Domain.Common;
using ContentService.Domain.Enums;
using ContentService.Domain.Exceptions;

namespace ContentService.Domain.Entities;

/// <summary>
/// Bir içerik kaydı. <see cref="UserId"/> ile User Service'teki bir kullanıcıya bağlanır.
/// Yayın yaşam döngüsü (taslak → yayında → arşiv) entity'nin kendi metotlarıyla yönetilir.
/// </summary>
public class Content : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    /// <summary>URL dostu, tekil tanımlayıcı. Oluşturulurken üretilir, sonradan değişmez.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>İçeriğin dili (ISO 639-1, ör. "tr", "en"). Oluşturulurken belirlenir.</summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Aynı içeriğin farklı dil versiyonlarını birbirine bağlayan kimlik.
    /// İlk versiyon yeni bir grup başlatır; çeviriler aynı grup kimliğini paylaşır.
    /// </summary>
    public Guid TranslationGroupId { get; set; }

    /// <summary>İçeriğin sahibi olan kullanıcının kimliği (User Service'te doğrulanır).</summary>
    public Guid UserId { get; set; }

    public ContentStatus Status { get; set; } = ContentStatus.Draft;

    /// <summary>İçeriğin ilk yayınlandığı an (henüz yayınlanmadıysa null).</summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>İçeriğe ekli medya dosyaları (içerik aggregate'inin parçası).</summary>
    public List<MediaAttachment> MediaAttachments { get; set; } = new();

    /// <summary>Taslak/arşiv durumundaki içeriği yayına alır.</summary>
    public void Publish()
    {
        if (Status == ContentStatus.Published)
        {
            throw new DomainException("İçerik zaten yayında.");
        }

        Status = ContentStatus.Published;
        PublishedAt ??= DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Yayındaki/taslak içeriği arşivler.</summary>
    public void Archive()
    {
        if (Status == ContentStatus.Archived)
        {
            throw new DomainException("İçerik zaten arşivlenmiş.");
        }

        Status = ContentStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Başlık ve gövdeyi günceller (slug ve durum değişmez).</summary>
    public void UpdateDetails(string title, string body)
    {
        Title = title;
        Body = body;
        UpdatedAt = DateTime.UtcNow;
    }
}
