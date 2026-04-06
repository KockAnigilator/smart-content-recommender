using Microsoft.EntityFrameworkCore;
using SmartContentRecommender.Application.UserActions.Interfaces;
using SmartContentRecommender.Application.UserActions.Models;
using SmartContentRecommender.Domain.Entities;
using SmartContentRecommender.Infrastructure.Persistence;

namespace SmartContentRecommender.Infrastructure.UserActions;

public class UserActionService : IUserActionService
{
    private readonly ApplicationDbContext _dbContext;

    public UserActionService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> LogActionAsync(Guid userId, LogUserActionRequest request, CancellationToken cancellationToken = default)
    {
        var userExists = await _dbContext.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!userExists)
        {
            return false;
        }

        var contentExists = await _dbContext.Contents.AnyAsync(c => c.Id == request.ContentId, cancellationToken);
        if (!contentExists)
        {
            return false;
        }

        var action = new UserAction
        {
            UserId = userId,
            ContentId = request.ContentId,
            Type = request.Type,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.UserActions.Add(action);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IReadOnlyList<UserActionHistoryItemDto>> GetUserHistoryAsync(
        Guid userId,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 500);

        var history = await _dbContext.UserActions
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(safeLimit)
            .Select(a => new UserActionHistoryItemDto
            {
                Id = a.Id,
                ContentId = a.ContentId,
                Type = a.Type,
                CreatedAtUtc = a.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return history;
    }
}

