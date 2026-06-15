using ContentService.Application.DTOs;
using ContentService.Domain.Enums;

namespace ContentService.Application.Interfaces;

public interface IContentService
{
    Task<IReadOnlyList<ContentDto>> GetAllAsync(ContentStatus? status = null, CancellationToken cancellationToken = default);

    Task<ContentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ContentDto> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<ContentDto> CreateAsync(CreateContentRequest request, CancellationToken cancellationToken = default);

    Task<ContentDto> UpdateAsync(Guid id, UpdateContentRequest request, CancellationToken cancellationToken = default);

    Task<ContentDto> PublishAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ContentDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
