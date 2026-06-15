using ContentService.Domain.Entities;
using ContentService.Domain.Enums;

namespace ContentService.Application.Interfaces;

public interface IContentRepository
{
    /// <summary>İçerikleri (opsiyonel olarak durum ve dile göre filtreleyerek) listeler.</summary>
    Task<IReadOnlyList<Content>> GetAllAsync(ContentStatus? status = null, string? language = null, CancellationToken cancellationToken = default);

    Task<Content?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Content?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Bir çeviri grubunda en az bir içerik var mı?</summary>
    Task<bool> TranslationGroupExistsAsync(Guid translationGroupId, CancellationToken cancellationToken = default);

    /// <summary>Bir çeviri grubunda belirtilen dil zaten var mı?</summary>
    Task<bool> ExistsInGroupWithLanguageAsync(Guid translationGroupId, string language, CancellationToken cancellationToken = default);

    /// <summary>Bir çeviri grubundaki tüm içerikleri (tüm dil versiyonları) getirir.</summary>
    Task<IReadOnlyList<Content>> GetByTranslationGroupAsync(Guid translationGroupId, CancellationToken cancellationToken = default);

    Task AddAsync(Content content, CancellationToken cancellationToken = default);

    Task UpdateAsync(Content content, CancellationToken cancellationToken = default);

    Task DeleteAsync(Content content, CancellationToken cancellationToken = default);
}
