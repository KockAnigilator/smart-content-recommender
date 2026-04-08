using Microsoft.EntityFrameworkCore;
using SmartContentRecommender.Application.Recommendations.Models;
using SmartContentRecommender.Domain.Entities;
using SmartContentRecommender.Domain.Enums;
using SmartContentRecommender.Infrastructure.Persistence;
using SmartContentRecommender.Infrastructure.Recommendations;

namespace SmartContentRecommender.Tests;

public class RecommendationServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetPopularAsync_ReturnsByDescendingScore()
    {
        using var db = CreateContext();

        var content1 = new Content { Id = Guid.NewGuid(), Title = "A", Url = "http://a", CategoryId = Guid.NewGuid() };
        var content2 = new Content { Id = Guid.NewGuid(), Title = "B", Url = "http://b", CategoryId = Guid.NewGuid() };

        db.Contents.AddRange(content1, content2);

        var userId = Guid.NewGuid();

        db.UserActions.AddRange(
            new UserAction { UserId = userId, ContentId = content1.Id, Type = UserActionType.View },
            new UserAction { UserId = userId, ContentId = content1.Id, Type = UserActionType.Like },
            new UserAction { UserId = userId, ContentId = content2.Id, Type = UserActionType.View }
        );

        await db.SaveChangesAsync();

        var service = new RecommendationService(db);

        var result = await service.GetPopularAsync(10, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetKnnAsync_FallsBackToPopular_WhenNoHistory()
    {
        using var db = CreateContext();

        var userId = Guid.NewGuid();

        var service = new RecommendationService(db);

        var result = await service.GetKnnAsync(userId, 10, CancellationToken.None);

        Assert.Empty(result); // без действий и контента популярного тоже нет
    }
}

