using SmartContentRecommender.Application.Content.Models;

namespace SmartContentRecommender.Application.Content.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CategoryItemDto?> CreateAsync(CreateNameRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

