using SmartContentRecommender.Application.UserActions.Models;

namespace SmartContentRecommender.Application.UserActions.Interfaces;

public interface IUserActionService
{
    Task<bool> LogActionAsync(Guid userId, LogUserActionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserActionHistoryItemDto>> GetUserHistoryAsync(Guid userId, int limit = 100, CancellationToken cancellationToken = default);
}

