using System.IO;
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
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    public string BaseUrl => _httpClient.BaseAddress?.ToString() ?? string.Empty;

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

    public async Task<List<CategoryItem>> GetCategoriesAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<CategoryItem>>("/api/categories", _jsonOptions) ?? [];
    }

    public async Task<List<TagItem>> GetTagsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<TagItem>>("/api/tags", _jsonOptions) ?? [];
    }

    public async Task<bool> CreateContentAsync(CreateContentRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/content", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateContentAsync(Guid id, UpdateContentRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/content/{id}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteContentAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"/api/content/{id}");
        return response.IsSuccessStatusCode;
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

    public async Task<List<RecommendationExplanationItem>> GetExplainAsync(string algorithm = "knn", int limit = 10)
    {
        var response = await _httpClient.GetAsync($"/api/recommendations/explain?algorithm={algorithm}&limit={limit}");
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return await response.Content.ReadFromJsonAsync<List<RecommendationExplanationItem>>(_jsonOptions) ?? [];
    }

    public async Task<InterestProfile?> GetInterestProfileAsync(int top = 5)
    {
        var response = await _httpClient.GetAsync($"/api/useractions/interest-profile?top={top}");
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<InterestProfile>(_jsonOptions);
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

    public async Task<RecommendationMetricsItem?> GetAdminMetricsAsync(Guid userId, string algorithm = "knn", int k = 10)
    {
        var response = await _httpClient.GetAsync($"/api/admin/metrics/recommendations?userId={userId}&algorithm={algorithm}&k={k}");
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<RecommendationMetricsItem>(_jsonOptions);
    }

    public async Task<string?> DownloadReportAsync(string format)
    {
        var response = await _httpClient.GetAsync($"/api/admin/reports/export/{format}");
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var ext = format.ToLowerInvariant() == "pdf" ? "pdf" : "csv";
        var fileName = $"defense-report-{DateTime.Now:yyyyMMdd-HHmmss}.{ext}";
        var targetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), fileName);
        await File.WriteAllBytesAsync(targetPath, bytes);
        return targetPath;
    }

    public async Task<DbOverview?> GetDbOverviewAsync()
    {
        var response = await _httpClient.GetAsync("/api/admin/db/overview");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<DbOverview>(_jsonOptions);
    }

    public async Task<List<DbUserRow>> GetDbUsersAsync()
    {
        var response = await _httpClient.GetAsync("/api/admin/db/users?limit=200");
        if (!response.IsSuccessStatusCode) return [];
        return await response.Content.ReadFromJsonAsync<List<DbUserRow>>(_jsonOptions) ?? [];
    }

    public async Task<List<CategoryItem>> GetDbCategoriesAsync()
    {
        var response = await _httpClient.GetAsync("/api/admin/db/categories?limit=200");
        if (!response.IsSuccessStatusCode) return [];
        return await response.Content.ReadFromJsonAsync<List<CategoryItem>>(_jsonOptions) ?? [];
    }

    public async Task<List<TagItem>> GetDbTagsAsync()
    {
        var response = await _httpClient.GetAsync("/api/admin/db/tags?limit=200");
        if (!response.IsSuccessStatusCode) return [];
        return await response.Content.ReadFromJsonAsync<List<TagItem>>(_jsonOptions) ?? [];
    }

    public async Task<List<DbContentRow>> GetDbContentsAsync()
    {
        var response = await _httpClient.GetAsync("/api/admin/db/contents?limit=200");
        if (!response.IsSuccessStatusCode) return [];
        return await response.Content.ReadFromJsonAsync<List<DbContentRow>>(_jsonOptions) ?? [];
    }

    public async Task<List<DbActionRow>> GetDbActionsAsync()
    {
        var response = await _httpClient.GetAsync("/api/admin/db/actions?limit=200");
        if (!response.IsSuccessStatusCode) return [];
        return await response.Content.ReadFromJsonAsync<List<DbActionRow>>(_jsonOptions) ?? [];
    }

    public async Task<bool> IsApiAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/content");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

