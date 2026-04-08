using Microsoft.AspNetCore.Http;

namespace SmartContentRecommender.WebClient.Services;

public class SessionTokenStore : ITokenStore
{
    private const string TokenKey = "jwt";
    private const string RoleKey = "role";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public SessionTokenStore(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetToken()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        return session?.GetString(TokenKey);
    }

    public void SetToken(string token)
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        session?.SetString(TokenKey, token);
    }

    public void Clear()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        session?.Remove(TokenKey);
        session?.Remove(RoleKey);
    }

    public string? GetRole()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        return session?.GetString(RoleKey);
    }

    public void SetRole(string role)
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        session?.SetString(RoleKey, role);
    }
}

