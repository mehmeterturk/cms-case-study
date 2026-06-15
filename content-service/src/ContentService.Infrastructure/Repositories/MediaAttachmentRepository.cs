using ContentService.Application.Interfaces;
using ContentService.Domain.Entities;
using ContentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentService.Infrastructure.Repositories;

public class MediaAttachmentRepository : IMediaAttachmentRepository
{
    private readonly AppDbContext _context;

    public MediaAttachmentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<MediaAttachment>> GetByContentIdAsync(Guid contentId, CancellationToken cancellationToken = default) =>
        await _context.MediaAttachments.AsNoTracking()
            .Where(m => m.ContentId == contentId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<MediaAttachment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.MediaAttachments.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task AddAsync(MediaAttachment media, CancellationToken cancellationToken = default)
    {
        await _context.MediaAttachments.AddAsync(media, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(MediaAttachment media, CancellationToken cancellationToken = default)
    {
        _context.MediaAttachments.Remove(media);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
