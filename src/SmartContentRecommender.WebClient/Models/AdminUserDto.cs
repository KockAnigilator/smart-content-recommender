namespace SmartContentRecommender.WebClient.Models;

public class AdminUserItem
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class ChangeUserRoleRequest
{
    // API принимает число 0/1, но для простоты передаем string и клиент сам переведет.
    public string Role { get; set; } = string.Empty;
}

public class SetUserBlockRequest
{
    public bool IsBlocked { get; set; }
}

