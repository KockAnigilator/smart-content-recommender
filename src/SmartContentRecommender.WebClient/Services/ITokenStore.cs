namespace SmartContentRecommender.WebClient.Services;

public interface ITokenStore
{
    string? GetToken();
    void SetToken(string token);
    void Clear();

    string? GetRole();
    void SetRole(string role);
}

