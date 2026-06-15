namespace ContentService.Application.Interfaces;

/// <summary>
/// Dosya/medya depolama soyutlaması (port). İçeriğe eklenen medya dosyalarının
/// fiziksel olarak nereye yazılacağından bağımsız bir sözleşmedir. Infrastructure
/// katmanında bu "sokete" farklı sağlayıcılar takılır: yerel disk, Amazon S3,
/// Azure Blob Storage. Sağlayıcı yapılandırma ile seçilir (Dependency Inversion).
/// </summary>
public interface IFileStorage
{
    /// <summary>Veriyi verilen anahtarla depolar.</summary>
    Task SaveAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Depodaki veriyi okunabilir bir akış olarak döndürür.</summary>
    Task<Stream> GetAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>Depodaki veriyi siler (yoksa sessizce geçer).</summary>
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
