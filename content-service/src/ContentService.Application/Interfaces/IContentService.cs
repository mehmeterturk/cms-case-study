using ContentService.Application.DTOs;
using ContentService.Application.Models;
using ContentService.Domain.Enums;

namespace ContentService.Application.Interfaces;

public interface IContentService
{
    Task<IReadOnlyList<ContentDto>> GetAllAsync(ContentStatus? status = null, CancellationToken cancellationToken = default);

    Task<ContentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ContentDto> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>İçerik oluşturur ve isteğe bağlı medya dosyalarını ekler.</summary>
    Task<ContentDto> CreateAsync(CreateContentRequest request, IReadOnlyList<FileUpload> files, CancellationToken cancellationToken = default);

    /// <summary>İçeriği günceller ve isteğe bağlı yeni medya dosyaları ekler (mevcutlar korunur).</summary>
    Task<ContentDto> UpdateAsync(Guid id, UpdateContentRequest request, IReadOnlyList<FileUpload> files, CancellationToken cancellationToken = default);

    Task<ContentDto> PublishAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ContentDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>İçeriğe ait bir medya dosyasını indirir.</summary>
    Task<FileDownload> DownloadMediaAsync(Guid contentId, Guid mediaId, CancellationToken cancellationToken = default);

    /// <summary>İçeriğe ait bir medya dosyasını siler (depodan + veritabanından).</summary>
    Task DeleteMediaAsync(Guid contentId, Guid mediaId, CancellationToken cancellationToken = default);
}
