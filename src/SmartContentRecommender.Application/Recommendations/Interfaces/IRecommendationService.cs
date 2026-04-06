using SmartContentRecommender.Application.Recommendations.Models;

namespace SmartContentRecommender.Application.Recommendations.Interfaces;

public interface IRecommendationService
{
    Task<IReadOnlyList<RecommendationItemDto>> GetByCategoriesAsync(Guid userId, int limit = 10, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecommendationItemDto>> GetPopularAsync(int limit = 10, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecommendationItemDto>> GetKnnAsync(Guid userId, int limit = 10, CancellationToken cancellationToken = default);
}

