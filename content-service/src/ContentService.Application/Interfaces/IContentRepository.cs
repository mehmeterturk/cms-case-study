using ContentService.Domain.Entities;
using ContentService.Domain.Enums;

namespace ContentService.Application.Interfaces;

public interface IContentRepository
{
    /// <summary>İçerikleri (opsiyonel olarak duruma göre filtreleyerek) listeler.</summary>
    Task<IReadOnlyList<Content>> GetAllAsync(ContentStatus? status = null, CancellationToken cancellationToken = default);

    Task<Content?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Content?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task AddAsync(Content content, CancellationToken cancellationToken = default);

    Task UpdateAsync(Content content, CancellationToken cancellationToken = default);

    Task DeleteAsync(Content content, CancellationToken cancellationToken = default);
}
