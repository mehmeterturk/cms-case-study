using ContentService.Application.Interfaces;
using ContentService.Domain.Entities;
using ContentService.Domain.Enums;
using ContentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentService.Infrastructure.Repositories;

public class ContentRepository : IContentRepository
{
    private readonly AppDbContext _context;

    public ContentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Content>> GetAllAsync(ContentStatus? status = null, Language? language = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Contents.AsNoTracking().Include(c => c.MediaAttachments).AsQueryable();

        if (status is not null)
        {
            query = query.Where(c => c.Status == status);
        }

        if (language is not null)
        {
            query = query.Where(c => c.Language == language);
        }

        return await query.OrderByDescending(c => c.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<Content?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Contents.Include(c => c.MediaAttachments).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<Content?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        await _context.Contents.AsNoTracking().Include(c => c.MediaAttachments).FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);

    public async Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        await _context.Contents.AnyAsync(c => c.Slug == slug, cancellationToken);

    public async Task<bool> TranslationGroupExistsAsync(Guid translationGroupId, CancellationToken cancellationToken = default) =>
        await _context.Contents.AnyAsync(c => c.TranslationGroupId == translationGroupId, cancellationToken);

    public async Task<bool> ExistsInGroupWithLanguageAsync(Guid translationGroupId, Language language, CancellationToken cancellationToken = default) =>
        await _context.Contents.AnyAsync(c => c.TranslationGroupId == translationGroupId && c.Language == language, cancellationToken);

    public async Task<IReadOnlyList<Content>> GetByTranslationGroupAsync(Guid translationGroupId, CancellationToken cancellationToken = default) =>
        await _context.Contents.AsNoTracking().Include(c => c.MediaAttachments)
            .Where(c => c.TranslationGroupId == translationGroupId)
            .OrderBy(c => c.Language)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Content content, CancellationToken cancellationToken = default)
    {
        await _context.Contents.AddAsync(content, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Content content, CancellationToken cancellationToken = default)
    {
        _context.Contents.Update(content);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Content content, CancellationToken cancellationToken = default)
    {
        _context.Contents.Remove(content);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
