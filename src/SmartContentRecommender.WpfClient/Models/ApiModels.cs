using System.Text.Json.Serialization;

namespace SmartContentRecommender.WpfClient.Models;

public class AuthPayload
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthApiResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public AuthData? Data { get; set; }
}

public class AuthData
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}

public class MeResponse
{
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
}

public class ContentItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Url { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
}

public class RecommendationItem
{
    public Guid ContentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Url { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public double Score { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class UserActionLogPayload
{
    public Guid ContentId { get; set; }
    public int Type { get; set; }
}

public class AdminUserItem
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

