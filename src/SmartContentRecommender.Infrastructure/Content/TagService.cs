using Microsoft.EntityFrameworkCore;
using SmartContentRecommender.Application.Content.Interfaces;
using SmartContentRecommender.Application.Content.Models;
using SmartContentRecommender.Domain.Entities;
using SmartContentRecommender.Infrastructure.Persistence;

namespace SmartContentRecommender.Infrastructure.Content;

public class TagService : ITagService
{
    private readonly ApplicationDbContext _dbContext;

    public TagService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TagItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tags
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new TagItemDto { Id = t.Id, Name = t.Name })
            .ToListAsync(cancellationToken);
    }

    public async Task<TagItemDto?> CreateAsync(CreateNameRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var exists = await _dbContext.Tags.AnyAsync(t => t.Name.ToLower() == name.ToLower(), cancellationToken);
        if (exists)
        {
            return null;
        }

        var entity = new Tag { Name = name };
        _dbContext.Tags.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new TagItemDto { Id = entity.Id, Name = entity.Name };
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Tags.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        _dbContext.Tags.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

