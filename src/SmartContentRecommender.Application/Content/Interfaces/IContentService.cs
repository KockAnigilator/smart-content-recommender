using SmartContentRecommender.Application.Content.Models;

namespace SmartContentRecommender.Application.Content.Interfaces;

public interface IContentService
{
    Task<IReadOnlyList<ContentItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ContentItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ContentItemDto?> CreateAsync(CreateContentRequest request, CancellationToken cancellationToken = default);
    Task<ContentItemDto?> UpdateAsync(Guid id, UpdateContentRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

