using SmartContentRecommender.Application.Content.Models;

namespace SmartContentRecommender.Application.Content.Interfaces;

public interface ITagService
{
    Task<IReadOnlyList<TagItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TagItemDto?> CreateAsync(CreateNameRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

