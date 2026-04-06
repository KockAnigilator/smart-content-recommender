using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SmartContentRecommender.WpfClient.Models;

namespace SmartContentRecommender.WpfClient.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiClient(string baseUrl)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };
    }

    public void SetToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            return;
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<AuthApiResponse?> RegisterAsync(AuthPayload payload)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/register", payload);
        return await response.Content.ReadFromJsonAsync<AuthApiResponse>(_jsonOptions);
    }

    public async Task<AuthApiResponse?> LoginAsync(AuthPayload payload)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/login", payload);
        return await response.Content.ReadFromJsonAsync<AuthApiResponse>(_jsonOptions);
    }

    public async Task<MeResponse?> GetMeAsync()
    {
        return await _httpClient.GetFromJsonAsync<MeResponse>("/api/auth/me", _jsonOptions);
    }

    public async Task<List<ContentItem>> GetContentAsync()
    {
        var result = await _httpClient.GetFromJsonAsync<List<ContentItem>>("/api/content", _jsonOptions);
        return result ?? [];
    }

    public async Task<bool> LogActionAsync(Guid contentId, int actionType)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/useractions/log", new UserActionLogPayload
        {
            ContentId = contentId,
            Type = actionType
        });

        return response.IsSuccessStatusCode;
    }

    public async Task<List<RecommendationItem>> GetPopularAsync()
    {
        var result = await _httpClient.GetFromJsonAsync<List<RecommendationItem>>("/api/recommendations/popular", _jsonOptions);
        return result ?? [];
    }

    public async Task<List<RecommendationItem>> GetByCategoriesAsync()
    {
        var result = await _httpClient.GetFromJsonAsync<List<RecommendationItem>>("/api/recommendations/by-categories", _jsonOptions);
        return result ?? [];
    }

    public async Task<List<RecommendationItem>> GetKnnAsync()
    {
        var result = await _httpClient.GetFromJsonAsync<List<RecommendationItem>>("/api/recommendations/knn", _jsonOptions);
        return result ?? [];
    }

    public async Task<List<AdminUserItem>> GetAdminUsersAsync()
    {
        var result = await _httpClient.GetFromJsonAsync<List<AdminUserItem>>("/api/admin/users", _jsonOptions);
        return result ?? [];
    }

    public async Task<bool> ChangeUserRoleAsync(Guid userId, string role)
    {
        var roleValue = role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        var payload = JsonContent.Create(new { role = roleValue });
        var response = await _httpClient.PutAsync($"/api/admin/users/{userId}/role", payload);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> SetBlockedAsync(Guid userId, bool isBlocked)
    {
        var payload = JsonContent.Create(new { isBlocked });
        var response = await _httpClient.PutAsync($"/api/admin/users/{userId}/block", payload);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        var response = await _httpClient.DeleteAsync($"/api/admin/users/{userId}");
        return response.IsSuccessStatusCode;
    }
}

