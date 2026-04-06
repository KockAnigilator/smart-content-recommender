using SmartContentRecommender.Domain.Enums;

namespace SmartContentRecommender.Application.UserActions.Models;

public class LogUserActionRequest
{
    public Guid ContentId { get; set; }
    public UserActionType Type { get; set; }
}

