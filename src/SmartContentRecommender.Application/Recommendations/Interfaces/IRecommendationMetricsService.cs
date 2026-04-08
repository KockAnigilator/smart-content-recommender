using SmartContentRecommender.Application.Recommendations.Models;

namespace SmartContentRecommender.Application.Recommendations.Interfaces;

public interface IRecommendationMetricsService
{
    Task<RecommendationMetricsDto> CalculateAsync(
        Guid userId,
        string algorithm,
        IReadOnlyList<Guid> recommendedContentIds,
        int k,
        CancellationToken cancellationToken = default);
}

