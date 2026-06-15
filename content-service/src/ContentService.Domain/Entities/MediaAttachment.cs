using ContentService.Domain.Common;

namespace ContentService.Domain.Entities;

/// <summary>
/// Bir içeriğe eklenen medya dosyası (görsel, video, belge vb.). Dosyanın kendisi
/// bir depolama sağlayıcısında (yerel/S3/Azure) tutulur; bu kayıt yalnızca üst veriyi
/// ve depolama anahtarını (<see cref="StorageKey"/>) saklar.
/// </summary>
public class MediaAttachment : BaseEntity
{
    /// <summary>Bağlı olduğu içeriğin kimliği.</summary>
    public Guid ContentId { get; set; }

    /// <summary>Kullanıcının yüklediği orijinal dosya adı.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>MIME türü (ör. image/png).</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Dosyanın depolama sağlayıcısındaki anahtarı/yolu (sağlayıcıdan bağımsız).</summary>
    public string StorageKey { get; set; } = string.Empty;

    /// <summary>Dosya boyutu (byte).</summary>
    public long SizeBytes { get; set; }
}
