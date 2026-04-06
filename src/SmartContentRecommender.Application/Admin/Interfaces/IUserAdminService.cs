using SmartContentRecommender.Application.Admin.Models;
using SmartContentRecommender.Domain.Enums;

namespace SmartContentRecommender.Application.Admin.Interfaces;

public interface IUserAdminService
{
    Task<IReadOnlyList<UserAdminItemDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<bool> ChangeRoleAsync(Guid userId, UserRole role, CancellationToken cancellationToken = default);
    Task<bool> SetBlockedAsync(Guid userId, bool isBlocked, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

