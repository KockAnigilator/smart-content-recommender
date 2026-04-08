using SmartContentRecommender.Application.Analytics.Models;

namespace SmartContentRecommender.Application.Analytics.Interfaces;

public interface IAnalyticsService
{
    Task<InterestProfileDto> GetInterestProfileAsync(
        Guid userId,
        int top = 5,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecommendationExplanationDto>> ExplainRecommendationsAsync(
        Guid userId,
        string algorithm,
        int limit = 10,
        CancellationToken cancellationToken = default);

    Task<DefenseReportDto> BuildDefenseReportAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        int topUsers = 10,
        CancellationToken cancellationToken = default);
}

