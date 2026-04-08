using Microsoft.EntityFrameworkCore;
using SmartContentRecommender.Application.Recommendations.Interfaces;
using SmartContentRecommender.Application.Recommendations.Models;
using SmartContentRecommender.Domain.Enums;
using SmartContentRecommender.Infrastructure.Persistence;

namespace SmartContentRecommender.Infrastructure.Recommendations;

public class RecommendationMetricsService : IRecommendationMetricsService
{
    private readonly ApplicationDbContext _dbContext;

    public RecommendationMetricsService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RecommendationMetricsDto> CalculateAsync(
        Guid userId,
        string algorithm,
        IReadOnlyList<Guid> recommendedContentIds,
        int k,
        CancellationToken cancellationToken = default)
    {
        var topK = recommendedContentIds.Take(k).ToList();

        var result = new RecommendationMetricsDto
        {
            UserId = userId,
            Algorithm = algorithm,
            K = topK.Count
        };

        if (topK.Count == 0)
        {
            return result;
        }

        var relevantContentIds = await _dbContext.UserActions
            .AsNoTracking()
            .Where(a => a.UserId == userId &&
                        (a.Type == UserActionType.Like || a.Type == UserActionType.Click))
            .Select(a => a.ContentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var relevantSet = relevantContentIds.ToHashSet();

        if (relevantSet.Count == 0)
        {
            return result;
        }

        var relevantInTopK = topK.Count(id => relevantSet.Contains(id));

        result.PrecisionAtK = topK.Count == 0 ? 0 : (double)relevantInTopK / topK.Count;
        result.RecallAtK = (double)relevantInTopK / relevantSet.Count;

        // NDCG@K: релевантные элементы имеют gain 1, остальные 0.
        double dcg = 0;
        for (var i = 0; i < topK.Count; i++)
        {
            if (!relevantSet.Contains(topK[i]))
            {
                continue;
            }

            dcg += 1.0 / Math.Log2(i + 2); // позиции считаем с 1
        }

        var idealRelCount = Math.Min(relevantSet.Count, topK.Count);
        double idcg = 0;
        for (var i = 0; i < idealRelCount; i++)
        {
            idcg += 1.0 / Math.Log2(i + 2);
        }

        result.NdcgAtK = idcg == 0 ? 0 : dcg / idcg;

        return result;
    }
}

