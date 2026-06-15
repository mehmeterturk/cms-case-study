using ContentService.Application.Interfaces;
using ContentService.Domain.Entities;
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

    public async Task<IReadOnlyList<Content>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Contents.AsNoTracking().OrderByDescending(c => c.CreatedAt).ToListAsync(cancellationToken);

    public async Task<Content?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Contents.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

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
