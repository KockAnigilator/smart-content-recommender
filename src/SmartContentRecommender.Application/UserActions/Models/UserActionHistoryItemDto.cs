using SmartContentRecommender.Domain.Enums;

namespace SmartContentRecommender.Application.UserActions.Models;

public class UserActionHistoryItemDto
{
    public Guid Id { get; set; }
    public Guid ContentId { get; set; }
    public UserActionType Type { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

