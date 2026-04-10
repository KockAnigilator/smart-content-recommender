using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartContentRecommender.Application.Auth.Models;
using SmartContentRecommender.Domain.Entities;
using SmartContentRecommender.Domain.Enums;
using SmartContentRecommender.Infrastructure.Auth;
using SmartContentRecommender.Infrastructure.Persistence;

namespace SmartContentRecommender.Tests;

public class AuthServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static AuthService CreateService(ApplicationDbContext db)
    {
        var jwtOptions = Options.Create(new JwtOptions
        {
            Issuer = "test",
            Audience = "test",
            Key = "VeryLongSuperSecretKeyForTests123456!",
            ExpiresHours = 1
        });

        return new AuthService(db, jwtOptions);
    }

    [Fact]
    public async Task RegisterAsync_CreatesUser()
    {
        using var db = CreateContext();
        var service = CreateService(db);

        var result = await service.RegisterAsync(new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Strong1!"
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(db.Users);
    }

    [Fact]
    public async Task LoginAsync_FailsForBlockedUser()
    {
        using var db = CreateContext();
        var service = CreateService(db);

        var user = new User
        {
            Email = "blocked@example.com",
            Role = UserRole.User,
            IsBlocked = true
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await service.LoginAsync(new LoginRequest
        {
            Email = "blocked@example.com",
            Password = "Any1!"
        }, CancellationToken.None);

        Assert.False(result.IsSuccess);
    }
}

