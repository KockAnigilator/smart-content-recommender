using Microsoft.EntityFrameworkCore;
using SmartContentRecommender.Application.Analytics.Interfaces;
using SmartContentRecommender.Application.Analytics.Models;
using SmartContentRecommender.Application.Recommendations.Interfaces;
using SmartContentRecommender.Domain.Enums;
using SmartContentRecommender.Infrastructure.Persistence;

namespace SmartContentRecommender.Infrastructure.Analytics;

public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IRecommendationService _recommendationService;
    private readonly IRecommendationMetricsService _recommendationMetricsService;

    public AnalyticsService(
        ApplicationDbContext dbContext,
        IRecommendationService recommendationService,
        IRecommendationMetricsService recommendationMetricsService)
    {
        _dbContext = dbContext;
        _recommendationService = recommendationService;
        _recommendationMetricsService = recommendationMetricsService;
    }

    public async Task<InterestProfileDto> GetInterestProfileAsync(
        Guid userId,
        int top = 5,
        CancellationToken cancellationToken = default)
    {
        var safeTop = Math.Clamp(top, 1, 20);

        var actions = await _dbContext.UserActions
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .ToListAsync(cancellationToken);

        var totalActions = actions.Count;

        var categoryScores = await _dbContext.UserActions
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Join(
                _dbContext.Contents.AsNoTracking().Include(c => c.Category),
                action => action.ContentId,
                content => content.Id,
                (action, content) => new
                {
                    CategoryName = content.Category != null ? content.Category.Name : "Unknown",
                    Weight = action.Type == UserActionType.Like
                        ? 3
                        : action.Type == UserActionType.Click
                            ? 2
                            : 1
                })
            .GroupBy(x => x.CategoryName)
            .Select(g => new InterestItemDto
            {
                Name = g.Key,
                Score = g.Sum(x => x.Weight)
            })
            .OrderByDescending(x => x.Score)
            .Take(safeTop)
            .ToListAsync(cancellationToken);

        var tagScores = await _dbContext.UserActions
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Join(
                _dbContext.ContentTags.AsNoTracking().Include(ct => ct.Tag),
                action => action.ContentId,
                ct => ct.ContentId,
                (action, ct) => new
                {
                    TagName = ct.Tag != null ? ct.Tag.Name : "Unknown",
                    Weight = action.Type == UserActionType.Like
                        ? 3
                        : action.Type == UserActionType.Click
                            ? 2
                            : 1
                })
            .GroupBy(x => x.TagName)
            .Select(g => new InterestItemDto
            {
                Name = g.Key,
                Score = g.Sum(x => x.Weight)
            })
            .OrderByDescending(x => x.Score)
            .Take(safeTop)
            .ToListAsync(cancellationToken);

        return new InterestProfileDto
        {
            UserId = userId,
            TotalActions = totalActions,
            TopCategories = categoryScores,
            TopTags = tagScores
        };
    }

    public async Task<IReadOnlyList<RecommendationExplanationDto>> ExplainRecommendationsAsync(
        Guid userId,
        string algorithm,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 50);
        var normalizedAlgorithm = algorithm.Trim().ToLowerInvariant();

        var recommendations = normalizedAlgorithm switch
        {
            "popular" => await _recommendationService.GetPopularAsync(safeLimit, cancellationToken),
            "by-categories" => await _recommendationService.GetByCategoriesAsync(userId, safeLimit, cancellationToken),
            _ => await _recommendationService.GetKnnAsync(userId, safeLimit, cancellationToken)
        };

        if (recommendations.Count == 0)
        {
            return [];
        }

        var contentIds = recommendations.Select(r => r.ContentId).ToHashSet();
        var maxFinalScore = recommendations.Max(r => r.Score);
        if (maxFinalScore <= 0) maxFinalScore = 1;

        var popularityMap = await _dbContext.UserActions
            .AsNoTracking()
            .Where(a => contentIds.Contains(a.ContentId))
            .GroupBy(a => a.ContentId)
            .Select(g => new
            {
                ContentId = g.Key,
                Score = g.Sum(x => x.Type == UserActionType.Like
                    ? 3
                    : x.Type == UserActionType.Click
                        ? 2
                        : 1)
            })
            .ToDictionaryAsync(x => x.ContentId, x => x.Score, cancellationToken);

        var maxPopularity = popularityMap.Count == 0 ? 1 : Math.Max(1, popularityMap.Max(x => x.Value));

        var topCategories = await _dbContext.UserActions
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Join(
                _dbContext.Contents.AsNoTracking(),
                action => action.ContentId,
                content => content.Id,
                (action, content) => new
                {
                    content.CategoryId,
                    Weight = action.Type == UserActionType.Like
                        ? 3
                        : action.Type == UserActionType.Click
                            ? 2
                            : 1
                })
            .GroupBy(x => x.CategoryId)
            .Select(g => new { CategoryId = g.Key, Score = g.Sum(x => x.Weight) })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Score, cancellationToken);

        var maxCategoryScore = topCategories.Count == 0 ? 1 : Math.Max(1, topCategories.Max(x => x.Value));

        var categoryByContent = await _dbContext.Contents
            .AsNoTracking()
            .Where(c => contentIds.Contains(c.Id))
            .Select(c => new { c.Id, c.CategoryId })
            .ToDictionaryAsync(x => x.Id, x => x.CategoryId, cancellationToken);

        return recommendations.Select(r =>
        {
            var popularityFactor = popularityMap.TryGetValue(r.ContentId, out var popularityScore)
                ? (double)popularityScore / maxPopularity
                : 0;

            var categoryFactor = 0.0;
            if (categoryByContent.TryGetValue(r.ContentId, out var categoryId) &&
                topCategories.TryGetValue(categoryId, out var categoryScore))
            {
                categoryFactor = (double)categoryScore / maxCategoryScore;
            }

            var knnFactor = normalizedAlgorithm == "knn" ? r.Score / maxFinalScore : 0;

            return new RecommendationExplanationDto
            {
                ContentId = r.ContentId,
                Title = r.Title,
                CategoryName = r.CategoryName,
                FinalScore = r.Score,
                PopularityFactor = Math.Round(popularityFactor, 4),
                CategoryAffinityFactor = Math.Round(categoryFactor, 4),
                KnnSimilarityFactor = Math.Round(knnFactor, 4),
                Reason = r.Reason
            };
        }).ToList();
    }

    public async Task<DefenseReportDto> BuildDefenseReportAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        int topUsers = 10,
        CancellationToken cancellationToken = default)
    {
        var report = new DefenseReportDto
        {
            GeneratedAtUtc = DateTime.UtcNow
        };

        var usersQuery = _dbContext.Users.AsNoTracking();
        var actionsQuery = _dbContext.UserActions.AsNoTracking();

        if (fromUtc.HasValue)
        {
            actionsQuery = actionsQuery.Where(a => a.CreatedAtUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            actionsQuery = actionsQuery.Where(a => a.CreatedAtUtc <= toUtc.Value);
        }

        report.UsersCount = await usersQuery.CountAsync(cancellationToken);
        report.ActionsCount = await actionsQuery.CountAsync(cancellationToken);

        var safeTopUsers = Math.Clamp(topUsers, 1, 50);

        report.Users = await _dbContext.Users
            .AsNoTracking()
            .Select(u => new ReportUserRowDto
            {
                UserId = u.Id,
                Email = u.Email,
                Role = u.Role.ToString(),
                IsBlocked = u.IsBlocked,
                ActionsCount = u.Actions.Count
            })
            .OrderByDescending(x => x.ActionsCount)
            .Take(safeTopUsers)
            .ToListAsync(cancellationToken);

        report.RecentActions = await _dbContext.UserActions
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(200)
            .Select(a => new ReportActionRowDto
            {
                UserEmail = a.User != null ? a.User.Email : "unknown",
                ContentTitle = a.Content != null ? a.Content.Title : "unknown",
                ActionType = a.Type.ToString(),
                CreatedAtUtc = a.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var metricsRows = new List<ReportMetricRowDto>();

        foreach (var user in report.Users.Take(5))
        {
            var recs = await _recommendationService.GetKnnAsync(user.UserId, 10, cancellationToken);
            var metrics = await _recommendationMetricsService.CalculateAsync(
                user.UserId,
                "knn",
                recs.Select(r => r.ContentId).ToList(),
                10,
                cancellationToken);

            metricsRows.Add(new ReportMetricRowDto
            {
                UserId = user.UserId,
                Algorithm = metrics.Algorithm,
                K = metrics.K,
                PrecisionAtK = metrics.PrecisionAtK,
                RecallAtK = metrics.RecallAtK,
                NdcgAtK = metrics.NdcgAtK
            });
        }

        report.RecommendationMetrics = metricsRows;

        return report;
    }

}

