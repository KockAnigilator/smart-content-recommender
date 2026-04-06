using Microsoft.EntityFrameworkCore;
using SmartContentRecommender.Application.Admin.Interfaces;
using SmartContentRecommender.Application.Admin.Models;
using SmartContentRecommender.Domain.Enums;
using SmartContentRecommender.Infrastructure.Persistence;

namespace SmartContentRecommender.Infrastructure.Admin;

public class UserAdminService : IUserAdminService
{
    private readonly ApplicationDbContext _dbContext;

    public UserAdminService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<UserAdminItemDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .OrderBy(u => u.CreatedAtUtc)
            .Select(u => new UserAdminItemDto
            {
                Id = u.Id,
                Email = u.Email,
                Role = u.Role,
                IsBlocked = u.IsBlocked,
                CreatedAtUtc = u.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ChangeRoleAsync(Guid userId, UserRole role, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.Role = role;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetBlockedAsync(Guid userId, bool isBlocked, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.IsBlocked = isBlocked;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

