using Microsoft.EntityFrameworkCore;
using SmartContentRecommender.Domain.Entities;
using SmartContentRecommender.Domain.Enums;
using SmartContentRecommender.Infrastructure.Persistence;
using SmartContentRecommender.Infrastructure.Recommendations;

namespace SmartContentRecommender.Tests;

public class RecommendationMetricsServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CalculateAsync_ComputesPrecisionAndRecall()
    {
        using var db = CreateContext();

        var userId = Guid.NewGuid();
        var c1 = Guid.NewGuid();
        var c2 = Guid.NewGuid();

        db.UserActions.AddRange(
            new UserAction { UserId = userId, ContentId = c1, Type = UserActionType.Like },
            new UserAction { UserId = userId, ContentId = c2, Type = UserActionType.Click }
        );

        await db.SaveChangesAsync();

        var service = new RecommendationMetricsService(db);

        var recommended = new List<Guid> { c1, Guid.NewGuid(), c2 };

        var metrics = await service.CalculateAsync(userId, "knn", recommended, 3, CancellationToken.None);

        Assert.Equal(3, metrics.K);
        Assert.True(metrics.PrecisionAtK > 0);
        Assert.True(metrics.RecallAtK > 0);
        Assert.True(metrics.NdcgAtK > 0);
    }
}

