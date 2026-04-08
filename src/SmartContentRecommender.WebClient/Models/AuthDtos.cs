namespace SmartContentRecommender.WebClient.Models;

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

