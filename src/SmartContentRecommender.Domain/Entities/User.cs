using SmartContentRecommender.Domain.Common;
using SmartContentRecommender.Domain.Enums;

namespace SmartContentRecommender.Domain.Entities;

public class User : EntityBase
{
    // Логин/почта для входа
    public string Email { get; set; } = string.Empty;

    // Хэш пароля (НЕ хранить пароль в открытом виде)
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.User;
    public bool IsBlocked { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // История действий пользователя
    public List<UserAction> Actions { get; set; } = new();
}

