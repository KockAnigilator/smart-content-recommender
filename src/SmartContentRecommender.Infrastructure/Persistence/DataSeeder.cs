using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartContentRecommender.Domain.Entities;
using SmartContentRecommender.Domain.Enums;

namespace SmartContentRecommender.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);

        var passwordHasher = new PasswordHasher<User>();

        var admin = await EnsureUserAsync(dbContext, passwordHasher, "admin@local", "Admin123!", UserRole.Admin, cancellationToken);
        var user1 = await EnsureUserAsync(dbContext, passwordHasher, "user1@local", "User123!", UserRole.User, cancellationToken);
        var user2 = await EnsureUserAsync(dbContext, passwordHasher, "user2@local", "User123!", UserRole.User, cancellationToken);

        var categories = await EnsureCategoriesAsync(dbContext, cancellationToken);
        var tags = await EnsureTagsAsync(dbContext, cancellationToken);
        var contents = await EnsureContentAsync(dbContext, categories, tags, cancellationToken);

        await EnsureUserActionsAsync(dbContext, user1, user2, contents, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<User> EnsureUserAsync(
        ApplicationDbContext dbContext,
        PasswordHasher<User> passwordHasher,
        string email,
        string password,
        UserRole role,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is not null)
        {
            return user;
        }

        user = new User
        {
            Email = email,
            Role = role,
            IsBlocked = false
        };
        user.PasswordHash = passwordHasher.HashPassword(user, password);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    private static async Task<List<Category>> EnsureCategoriesAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var names = new[] { "Technology", "Science", "Education", "Business", "Health" };
        foreach (var name in names)
        {
            if (!await dbContext.Categories.AnyAsync(c => c.Name == name, cancellationToken))
            {
                dbContext.Categories.Add(new Category { Name = name });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await dbContext.Categories.OrderBy(c => c.Name).ToListAsync(cancellationToken);
    }

    private static async Task<List<Tag>> EnsureTagsAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var names = new[] { "ai", "dotnet", "sql", "ml", "startup", "fitness" };
        foreach (var name in names)
        {
            if (!await dbContext.Tags.AnyAsync(t => t.Name == name, cancellationToken))
            {
                dbContext.Tags.Add(new Tag { Name = name });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await dbContext.Tags.OrderBy(t => t.Name).ToListAsync(cancellationToken);
    }

    private static async Task<List<Domain.Entities.Content>> EnsureContentAsync(
        ApplicationDbContext dbContext,
        List<Category> categories,
        List<Tag> tags,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Contents.AnyAsync(cancellationToken))
        {
            return await dbContext.Contents.Include(c => c.ContentTags).ToListAsync(cancellationToken);
        }

        var tech = categories.First(c => c.Name == "Technology");
        var science = categories.First(c => c.Name == "Science");
        var education = categories.First(c => c.Name == "Education");

        var contentItems = new List<Domain.Entities.Content>
        {
            new() { Title = "Intro to ASP.NET Core", Description = "Quick start for Web API", Url = "https://example.com/aspnet", CategoryId = tech.Id },
            new() { Title = "Machine Learning Basics", Description = "ML concepts for beginners", Url = "https://example.com/ml", CategoryId = science.Id },
            new() { Title = "PostgreSQL for Students", Description = "SQL and relational design", Url = "https://example.com/postgres", CategoryId = education.Id },
            new() { Title = "Clean Architecture Guide", Description = "Practical layered architecture", Url = "https://example.com/clean", CategoryId = tech.Id },
            new() { Title = "Statistics Essentials", Description = "Probability and analytics", Url = "https://example.com/stats", CategoryId = science.Id }
        };

        dbContext.Contents.AddRange(contentItems);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var content in contentItems)
        {
            dbContext.ContentTags.Add(new ContentTag
            {
                ContentId = content.Id,
                TagId = tags.First().Id
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return contentItems;
    }

    private static async Task EnsureUserActionsAsync(
        ApplicationDbContext dbContext,
        User user1,
        User user2,
        List<Domain.Entities.Content> contents,
        CancellationToken cancellationToken)
    {
        if (await dbContext.UserActions.AnyAsync(cancellationToken))
        {
            return;
        }

        var actions = new List<UserAction>
        {
            new() { UserId = user1.Id, ContentId = contents[0].Id, Type = UserActionType.View },
            new() { UserId = user1.Id, ContentId = contents[0].Id, Type = UserActionType.Like },
            new() { UserId = user1.Id, ContentId = contents[1].Id, Type = UserActionType.Click },
            new() { UserId = user1.Id, ContentId = contents[3].Id, Type = UserActionType.View },
            new() { UserId = user2.Id, ContentId = contents[1].Id, Type = UserActionType.Like },
            new() { UserId = user2.Id, ContentId = contents[2].Id, Type = UserActionType.View },
            new() { UserId = user2.Id, ContentId = contents[4].Id, Type = UserActionType.Click }
        };

        dbContext.UserActions.AddRange(actions);
    }
}

