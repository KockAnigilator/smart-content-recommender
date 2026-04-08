using Microsoft.EntityFrameworkCore;
using SmartContentRecommender.Application.Recommendations.Interfaces;
using SmartContentRecommender.Application.Recommendations.Models;
using SmartContentRecommender.Domain.Entities;
using SmartContentRecommender.Domain.Enums;
using SmartContentRecommender.Infrastructure.Persistence;

namespace SmartContentRecommender.Infrastructure.Recommendations;

public class RecommendationService : IRecommendationService
{
    private readonly ApplicationDbContext _dbContext;

    private static readonly TimeSpan KnnHistoryWindow = TimeSpan.FromDays(90);
    private const int MaxNeighbors = 5;

    public RecommendationService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<RecommendationItemDto>> GetByCategoriesAsync(
        Guid userId,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = NormalizeLimit(limit);

        var seenContentIds = await GetSeenContentIdsAsync(userId, cancellationToken);

        // 1) Берем "интерес пользователя по категориям" из истории действий.
        var preferredCategories = await _dbContext.UserActions
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Join(
                _dbContext.Contents.AsNoTracking(),
                action => action.ContentId,
                content => content.Id,
                (action, content) => new { action, content.CategoryId })
            .GroupBy(x => x.CategoryId)
            .Select(g => new
            {
                CategoryId = g.Key,
                Score = g.Sum(x =>
                    x.action.Type == UserActionType.Like ? 3 :
                    x.action.Type == UserActionType.Click ? 2 : 1)
            })
            .OrderByDescending(x => x.Score)
            .Take(5)
            .ToListAsync(cancellationToken);

        if (preferredCategories.Count == 0)
        {
            // Если у пользователя нет истории — fallback на популярное.
            return await GetPopularAsync(safeLimit, cancellationToken);
        }

        var categoryScores = preferredCategories.ToDictionary(x => x.CategoryId, x => (double)x.Score);
        var targetCategoryIds = categoryScores.Keys.ToList();

        var candidates = await _dbContext.Contents
            .AsNoTracking()
            .Include(c => c.Category)
            .Where(c => targetCategoryIds.Contains(c.CategoryId) && !seenContentIds.Contains(c.Id))
            .OrderByDescending(c => c.CreatedAtUtc)
            .Take(500)
            .ToListAsync(cancellationToken);

        return candidates
            .Select(c => new RecommendationItemDto
            {
                ContentId = c.Id,
                Title = c.Title,
                Description = c.Description,
                Url = c.Url,
                CategoryName = c.Category?.Name ?? string.Empty,
                Score = categoryScores.GetValueOrDefault(c.CategoryId, 0),
                Reason = "Рекомендация по интересующим категориям"
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.ContentId)
            .Take(safeLimit)
            .ToList();
    }

    public async Task<IReadOnlyList<RecommendationItemDto>> GetPopularAsync(
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = NormalizeLimit(limit);

        // Популярность = сумма весов действий (view=1, click=2, like=3).
        var popularity = await _dbContext.UserActions
            .AsNoTracking()
            .GroupBy(a => a.ContentId)
            .Select(g => new
            {
                ContentId = g.Key,
                Score = g.Sum(x =>
                    x.Type == UserActionType.Like ? 3 :
                    x.Type == UserActionType.Click ? 2 : 1)
            })
            .OrderByDescending(x => x.Score)
            .Take(safeLimit)
            .ToListAsync(cancellationToken);

        if (popularity.Count == 0)
        {
            return [];
        }

        var scoreMap = popularity.ToDictionary(x => x.ContentId, x => (double)x.Score);
        var contentIds = scoreMap.Keys.ToList();

        var contents = await _dbContext.Contents
            .AsNoTracking()
            .Include(c => c.Category)
            .Where(c => contentIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        return contents
            .Select(c => new RecommendationItemDto
            {
                ContentId = c.Id,
                Title = c.Title,
                Description = c.Description,
                Url = c.Url,
                CategoryName = c.Category?.Name ?? string.Empty,
                Score = scoreMap.GetValueOrDefault(c.Id, 0),
                Reason = "Популярный контент"
            })
            .OrderByDescending(x => x.Score)
            .Take(safeLimit)
            .ToList();
    }

    public async Task<IReadOnlyList<RecommendationItemDto>> GetKnnAsync(
        Guid userId,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = NormalizeLimit(limit);
        var seenContentIds = await GetSeenContentIdsAsync(userId, cancellationToken);

        var currentVector = await GetUserVectorAsync(userId, cancellationToken);
        if (currentVector.Count == 0)
        {
            return await GetPopularAsync(safeLimit, cancellationToken);
        }

        // Упрощенный KNN: считаем cosine similarity между текущим пользователем и остальными.
        var cutoff = DateTime.UtcNow - KnnHistoryWindow;

        var otherUserIds = await _dbContext.UserActions
            .AsNoTracking()
            .Where(a => a.UserId != userId && a.CreatedAtUtc >= cutoff)
            .Select(a => a.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var similarities = new List<(Guid UserId, double Similarity)>();
        foreach (var otherUserId in otherUserIds)
        {
            var otherVector = await GetUserVectorAsync(otherUserId, cancellationToken);
            var similarity = CosineSimilarity(currentVector, otherVector);
            if (similarity > 0)
            {
                similarities.Add((otherUserId, similarity));
            }
        }

        var nearestUsers = similarities
            .OrderByDescending(x => x.Similarity)
            .Take(MaxNeighbors)
            .ToList();

        if (nearestUsers.Count == 0)
        {
            return await GetPopularAsync(safeLimit, cancellationToken);
        }

        var nearestUserIds = nearestUsers.Select(x => x.UserId).ToHashSet();
        var similarityMap = nearestUsers.ToDictionary(x => x.UserId, x => x.Similarity);

        var neighborActions = await _dbContext.UserActions
            .AsNoTracking()
            .Where(a => nearestUserIds.Contains(a.UserId)
                        && !seenContentIds.Contains(a.ContentId)
                        && a.CreatedAtUtc >= cutoff)
            .ToListAsync(cancellationToken);

        var scoredContent = neighborActions
            .GroupBy(a => a.ContentId)
            .Select(g =>
            {
                var score = g.Sum(a => GetActionWeight(a.Type) * similarityMap.GetValueOrDefault(a.UserId, 0));
                return new { ContentId = g.Key, Score = score };
            })
            .OrderByDescending(x => x.Score)
            .Take(safeLimit)
            .ToList();

        if (scoredContent.Count == 0)
        {
            return await GetPopularAsync(safeLimit, cancellationToken);
        }

        var scoreByContent = scoredContent.ToDictionary(x => x.ContentId, x => x.Score);
        var contentIds = scoreByContent.Keys.ToList();

        var contents = await _dbContext.Contents
            .AsNoTracking()
            .Include(c => c.Category)
            .Where(c => contentIds.Contains(c.Id))
            .ToListAsync(cancellationToken);
        
        // Небольшой бонус за попадание в любимые категории текущего пользователя.
        var topCategories = await _dbContext.UserActions
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Join(
                _dbContext.Contents.AsNoTracking(),
                a => a.ContentId,
                c => c.Id,
                (a, c) => new { a, c.CategoryId })
            .GroupBy(x => x.CategoryId)
            .Select(g => new
            {
                CategoryId = g.Key,
                Score = g.Sum(x =>
                    x.a.Type == UserActionType.Like ? 3 :
                    x.a.Type == UserActionType.Click ? 2 : 1)
            })
            .OrderByDescending(x => x.Score)
            .Take(3)
            .ToListAsync(cancellationToken);

        var categoryBonus = topCategories.ToDictionary(x => x.CategoryId, x => x.Score);

        foreach (var c in contents)
        {
            if (c.CategoryId != Guid.Empty &&
                categoryBonus.TryGetValue(c.CategoryId, out var bonus))
            {
                scoreByContent[c.Id] += bonus * 0.1;
            }
        }

        return contents
            .Select(c => new RecommendationItemDto
            {
                ContentId = c.Id,
                Title = c.Title,
                Description = c.Description,
                Url = c.Url,
                CategoryName = c.Category?.Name ?? string.Empty,
                Score = scoreByContent.GetValueOrDefault(c.Id, 0),
                Reason = "Похоже на действия похожих пользователей (KNN)"
            })
            .OrderByDescending(x => x.Score)
            .Take(safeLimit)
            .ToList();
    }

    private static int NormalizeLimit(int limit)
    {
        return Math.Clamp(limit, 1, 50);
    }

    private async Task<HashSet<Guid>> GetSeenContentIdsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var seen = await _dbContext.UserActions
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => a.ContentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return seen.ToHashSet();
    }

    private async Task<Dictionary<Guid, double>> GetUserVectorAsync(Guid userId, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow - KnnHistoryWindow;

        return await _dbContext.UserActions
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.CreatedAtUtc >= cutoff)
            .GroupBy(a => a.ContentId)
            .Select(g => new
            {
                ContentId = g.Key,
                Weight = g.Sum(x =>
                    x.Type == UserActionType.Like ? 3 :
                    x.Type == UserActionType.Click ? 2 : 1)
            })
            .ToDictionaryAsync(x => x.ContentId, x => (double)x.Weight, cancellationToken);
    }

    private static double CosineSimilarity(Dictionary<Guid, double> a, Dictionary<Guid, double> b)
    {
        if (a.Count == 0 || b.Count == 0)
        {
            return 0;
        }

        var dot = 0.0;
        foreach (var (contentId, aValue) in a)
        {
            if (b.TryGetValue(contentId, out var bValue))
            {
                dot += aValue * bValue;
            }
        }

        var normA = Math.Sqrt(a.Values.Sum(v => v * v));
        var normB = Math.Sqrt(b.Values.Sum(v => v * v));
        if (normA == 0 || normB == 0)
        {
            return 0;
        }

        return dot / (normA * normB);
    }

    private static int GetActionWeight(UserActionType type)
    {
        return type switch
        {
            UserActionType.View => 1,
            UserActionType.Click => 2,
            UserActionType.Like => 3,
            _ => 1
        };
    }
}

