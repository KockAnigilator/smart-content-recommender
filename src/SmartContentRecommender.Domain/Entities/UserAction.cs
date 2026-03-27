using SmartContentRecommender.Domain.Common;
using SmartContentRecommender.Domain.Enums;

namespace SmartContentRecommender.Domain.Entities;

public class UserAction : EntityBase
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid ContentId { get; set; }
    public Content? Content { get; set; }

    public UserActionType Type { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

