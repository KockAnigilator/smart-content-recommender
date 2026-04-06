using Microsoft.EntityFrameworkCore;
using SmartContentRecommender.Application.Content.Interfaces;
using SmartContentRecommender.Application.Content.Models;
using SmartContentRecommender.Domain.Entities;
using SmartContentRecommender.Infrastructure.Persistence;

namespace SmartContentRecommender.Infrastructure.Content;

public class ContentService : IContentService
{
    private readonly ApplicationDbContext _dbContext;

    public ContentService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ContentItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.Contents
            .AsNoTracking()
            .Include(c => c.Category)
            .Include(c => c.ContentTags)
            .ThenInclude(ct => ct.Tag)
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return items.Select(MapContent).ToList();
    }

    public async Task<ContentItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.Contents
            .AsNoTracking()
            .Include(c => c.Category)
            .Include(c => c.ContentTags)
            .ThenInclude(ct => ct.Tag)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return item is null ? null : MapContent(item);
    }

    public async Task<ContentItemDto?> CreateAsync(CreateContentRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _dbContext.Categories.AnyAsync(x => x.Id == request.CategoryId, cancellationToken))
        {
            return null;
        }

        var content = new Domain.Entities.Content
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Url = request.Url.Trim(),
            CategoryId = request.CategoryId,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Contents.Add(content);
        await AttachTagsAsync(content, request.TagIds, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(content.Id, cancellationToken);
    }

    public async Task<ContentItemDto?> UpdateAsync(Guid id, UpdateContentRequest request, CancellationToken cancellationToken = default)
    {
        var content = await _dbContext.Contents
            .Include(c => c.ContentTags)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (content is null)
        {
            return null;
        }

        if (!await _dbContext.Categories.AnyAsync(x => x.Id == request.CategoryId, cancellationToken))
        {
            return null;
        }

        content.Title = request.Title.Trim();
        content.Description = request.Description?.Trim();
        content.Url = request.Url.Trim();
        content.CategoryId = request.CategoryId;

        _dbContext.ContentTags.RemoveRange(content.ContentTags);
        await AttachTagsAsync(content, request.TagIds, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(content.Id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var content = await _dbContext.Contents.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (content is null)
        {
            return false;
        }

        _dbContext.Contents.Remove(content);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task AttachTagsAsync(Domain.Entities.Content content, IEnumerable<Guid> tagIds, CancellationToken cancellationToken)
    {
        var distinctTagIds = tagIds.Distinct().ToList();
        var existingTagIds = await _dbContext.Tags
            .Where(t => distinctTagIds.Contains(t.Id))
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        foreach (var tagId in existingTagIds)
        {
            content.ContentTags.Add(new ContentTag
            {
                ContentId = content.Id,
                TagId = tagId
            });
        }
    }

    private static ContentItemDto MapContent(Domain.Entities.Content content)
    {
        return new ContentItemDto
        {
            Id = content.Id,
            Title = content.Title,
            Description = content.Description,
            Url = content.Url,
            CategoryId = content.CategoryId,
            CategoryName = content.Category?.Name ?? string.Empty,
            Tags = content.ContentTags
                .Where(ct => ct.Tag is not null)
                .Select(ct => ct.Tag!.Name)
                .OrderBy(x => x)
                .ToList()
        };
    }
}

