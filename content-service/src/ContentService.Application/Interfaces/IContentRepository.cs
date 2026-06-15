using ContentService.Domain.Entities;

namespace ContentService.Application.Interfaces;

public interface IContentRepository
{
    Task<IReadOnlyList<Content>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Content?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Content content, CancellationToken cancellationToken = default);

    Task UpdateAsync(Content content, CancellationToken cancellationToken = default);

    Task DeleteAsync(Content content, CancellationToken cancellationToken = default);
}
