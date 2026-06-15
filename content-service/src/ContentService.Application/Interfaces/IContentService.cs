using ContentService.Application.DTOs;

namespace ContentService.Application.Interfaces;

public interface IContentService
{
    Task<IReadOnlyList<ContentDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ContentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ContentDto> CreateAsync(CreateContentRequest request, CancellationToken cancellationToken = default);

    Task<ContentDto> UpdateAsync(Guid id, UpdateContentRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
