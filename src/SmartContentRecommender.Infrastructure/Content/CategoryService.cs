using Microsoft.EntityFrameworkCore;
using SmartContentRecommender.Application.Content.Interfaces;
using SmartContentRecommender.Application.Content.Models;
using SmartContentRecommender.Domain.Entities;
using SmartContentRecommender.Infrastructure.Persistence;

namespace SmartContentRecommender.Infrastructure.Content;

public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _dbContext;

    public CategoryService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CategoryItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryItemDto { Id = c.Id, Name = c.Name })
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryItemDto?> CreateAsync(CreateNameRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var exists = await _dbContext.Categories.AnyAsync(c => c.Name.ToLower() == name.ToLower(), cancellationToken);
        if (exists)
        {
            return null;
        }

        var entity = new Category { Name = name };
        _dbContext.Categories.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CategoryItemDto { Id = entity.Id, Name = entity.Name };
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        _dbContext.Categories.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

