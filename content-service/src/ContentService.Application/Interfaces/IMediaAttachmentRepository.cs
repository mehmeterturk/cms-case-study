using ContentService.Domain.Entities;

namespace ContentService.Application.Interfaces;

public interface IMediaAttachmentRepository
{
    Task<IReadOnlyList<MediaAttachment>> GetByContentIdAsync(Guid contentId, CancellationToken cancellationToken = default);

    Task<MediaAttachment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(MediaAttachment media, CancellationToken cancellationToken = default);

    Task DeleteAsync(MediaAttachment media, CancellationToken cancellationToken = default);
}
