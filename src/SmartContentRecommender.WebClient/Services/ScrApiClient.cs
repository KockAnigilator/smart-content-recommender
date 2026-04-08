using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SmartContentRecommender.WebClient.Models;

namespace SmartContentRecommender.WebClient.Services;

public class ScrApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ITokenStore _tokenStore;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ScrApiClient(ITokenStore tokenStore, ApiSettings apiSettings, IConfiguration configuration)
    {
        _tokenStore = tokenStore;
        var baseUrl = configuration.GetValue<string?>("Api:BaseUrl") ?? apiSettings.BaseUrl;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    private string? Token => _tokenStore.GetToken();

    private void AddAuthHeader(HttpRequestMessage request)
    {
        if (string.IsNullOrWhiteSpace(Token))
        {
            request.Headers.Authorization = null;
            return;
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
    }

    public async Task<AuthApiResponse?> RegisterAsync(AuthPayload payload, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/register", payload, _jsonOptions, cancellationToken);
        return await response.Content.ReadFromJsonAsync<AuthApiResponse>(_jsonOptions, cancellationToken);
    }

    public async Task<AuthApiResponse?> LoginAsync(AuthPayload payload, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/login", payload, _jsonOptions, cancellationToken);
        return await response.Content.ReadFromJsonAsync<AuthApiResponse>(_jsonOptions, cancellationToken);
    }

    public async Task<MeResponse?> GetMeAsync(CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        AddAuthHeader(request);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<MeResponse>(_jsonOptions, cancellationToken);
    }

    public async Task<List<ContentItem>> GetContentAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<List<ContentItem>>("/api/content", _jsonOptions, cancellationToken) ?? [];
    }

    public async Task<bool> LogActionAsync(Guid contentId, int type, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/useractions/log")
        {
            Content = JsonContent.Create(new UserActionLogPayload { ContentId = contentId, Type = type })
        };
        AddAuthHeader(request);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<RecommendationItem>> GetPopularAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<List<RecommendationItem>>("/api/recommendations/popular", _jsonOptions, cancellationToken) ?? [];
    }

    public async Task<List<RecommendationItem>> GetByCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/recommendations/by-categories");
        AddAuthHeader(request);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return await response.Content.ReadFromJsonAsync<List<RecommendationItem>>(_jsonOptions, cancellationToken) ?? [];
    }

    public async Task<List<RecommendationItem>> GetKnnAsync(CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/recommendations/knn");
        AddAuthHeader(request);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return await response.Content.ReadFromJsonAsync<List<RecommendationItem>>(_jsonOptions, cancellationToken) ?? [];
    }

    public async Task<List<AdminUserItem>> GetAdminUsersAsync(CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/users");
        AddAuthHeader(request);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return await response.Content.ReadFromJsonAsync<List<AdminUserItem>>(_jsonOptions, cancellationToken) ?? [];
    }

    public async Task<bool> ChangeRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
    {
        var roleValue = role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/admin/users/{userId}/role")
        {
            Content = JsonContent.Create(new { role = roleValue })
        };
        AddAuthHeader(request);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> SetBlockedAsync(Guid userId, bool isBlocked, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/admin/users/{userId}/block")
        {
            Content = JsonContent.Create(new { isBlocked })
        };
        AddAuthHeader(request);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/users/{userId}");
        AddAuthHeader(request);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ApiAvailabilityCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/content");
            var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

