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
            Timeout = TimeSpan.FromSeconds(20)
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
        return await ReadAuthResponseAsync(response, "Ошибка регистрации.", cancellationToken);
    }

    public async Task<AuthApiResponse?> LoginAsync(AuthPayload payload, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/login", payload, _jsonOptions, cancellationToken);
        return await ReadAuthResponseAsync(response, "Ошибка авторизации.", cancellationToken);
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

    public async Task<InterestProfileVm?> GetInterestProfileAsync(CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/useractions/interest-profile?top=5");
        AddAuthHeader(request);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"interest-profile failed: {(int)response.StatusCode}", null, response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<InterestProfileVm>(_jsonOptions, cancellationToken);
    }

    public async Task<List<RecommendationExplanationVm>> GetExplainAsync(string algorithm = "knn", int limit = 10, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/recommendations/explain?algorithm={algorithm}&limit={limit}");
        AddAuthHeader(request);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"explain failed: {(int)response.StatusCode}", null, response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<List<RecommendationExplanationVm>>(_jsonOptions, cancellationToken) ?? [];
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

    public async Task<RecommendationMetricsVm?> GetAdminMetricsAsync(Guid userId, string algorithm = "knn", int k = 10, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/admin/metrics/recommendations?userId={userId}&algorithm={algorithm}&k={k}");
        AddAuthHeader(request);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<RecommendationMetricsVm>(_jsonOptions, cancellationToken);
    }

    public async Task<(byte[] Bytes, string ContentType, string FileName)?> DownloadAdminReportAsync(string format, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/admin/reports/export/{format}");
        AddAuthHeader(request);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var fileName = $"report.{format}";
        if (response.Content.Headers.ContentDisposition?.FileNameStar is not null)
        {
            fileName = response.Content.Headers.ContentDisposition.FileNameStar!;
        }
        else if (response.Content.Headers.ContentDisposition?.FileName is not null)
        {
            fileName = response.Content.Headers.ContentDisposition.FileName!.Trim('"');
        }

        return (bytes, contentType, fileName);
    }

    public async Task<List<CategoryItemVm>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<List<CategoryItemVm>>("/api/categories", _jsonOptions, cancellationToken) ?? [];
    }

    public async Task<List<TagItemVm>> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<List<TagItemVm>>("/api/tags", _jsonOptions, cancellationToken) ?? [];
    }

    public async Task<bool> CreateContentAsync(CreateContentVm vm, CancellationToken cancellationToken = default)
    {
        var tagIds = ParseGuids(vm.TagIdsCsv);
        var payload = new
        {
            title = vm.Title,
            description = vm.Description,
            url = vm.Url,
            categoryId = vm.CategoryId,
            tagIds
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/content")
        {
            Content = JsonContent.Create(payload)
        };
        AddAuthHeader(request);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateContentAsync(UpdateContentVm vm, CancellationToken cancellationToken = default)
    {
        var tagIds = ParseGuids(vm.TagIdsCsv);
        var payload = new
        {
            title = vm.Title,
            description = vm.Description,
            url = vm.Url,
            categoryId = vm.CategoryId,
            tagIds
        };

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/content/{vm.Id}")
        {
            Content = JsonContent.Create(payload)
        };
        AddAuthHeader(request);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteContentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/content/{id}");
        AddAuthHeader(request);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<DbOverviewVm?> GetAdminDbOverviewAsync(CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/db/overview");
        AddAuthHeader(request);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"admin db overview failed: {(int)response.StatusCode}", null, response.StatusCode);
        }
        return await response.Content.ReadFromJsonAsync<DbOverviewVm>(_jsonOptions, cancellationToken);
    }

    public async Task<List<DbUserRowVm>> GetAdminDbUsersAsync(CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/db/users?limit=200");
        AddAuthHeader(request);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"admin db users failed: {(int)response.StatusCode}", null, response.StatusCode);
        }
        return await response.Content.ReadFromJsonAsync<List<DbUserRowVm>>(_jsonOptions, cancellationToken) ?? [];
    }

    public async Task<List<DbCategoryRowVm>> GetAdminDbCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/db/categories?limit=200");
        AddAuthHeader(request);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"admin db categories failed: {(int)response.StatusCode}", null, response.StatusCode);
        }
        return await response.Content.ReadFromJsonAsync<List<DbCategoryRowVm>>(_jsonOptions, cancellationToken) ?? [];
    }

    public async Task<List<DbTagRowVm>> GetAdminDbTagsAsync(CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/db/tags?limit=200");
        AddAuthHeader(request);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"admin db tags failed: {(int)response.StatusCode}", null, response.StatusCode);
        }
        return await response.Content.ReadFromJsonAsync<List<DbTagRowVm>>(_jsonOptions, cancellationToken) ?? [];
    }

    public async Task<List<DbContentRowVm>> GetAdminDbContentsAsync(CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/db/contents?limit=200");
        AddAuthHeader(request);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"admin db contents failed: {(int)response.StatusCode}", null, response.StatusCode);
        }
        return await response.Content.ReadFromJsonAsync<List<DbContentRowVm>>(_jsonOptions, cancellationToken) ?? [];
    }

    public async Task<List<DbActionRowVm>> GetAdminDbActionsAsync(CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/db/actions?limit=200");
        AddAuthHeader(request);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"admin db actions failed: {(int)response.StatusCode}", null, response.StatusCode);
        }
        return await response.Content.ReadFromJsonAsync<List<DbActionRowVm>>(_jsonOptions, cancellationToken) ?? [];
    }

    private static List<Guid> ParseGuids(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return [];
        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToList();
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

    public async Task<bool> IsDevModeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/dev/status", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> GenerateDemoHistoryAsync(CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/dev/generate-demo-history");
        AddAuthHeader(request);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private async Task<AuthApiResponse> ReadAuthResponseAsync(
        HttpResponseMessage response,
        string fallbackMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            var typed = await response.Content.ReadFromJsonAsync<AuthApiResponse>(_jsonOptions, cancellationToken);
            if (typed is not null)
            {
                if (!response.IsSuccessStatusCode && typed.IsSuccess)
                {
                    typed.IsSuccess = false;
                }

                typed.Message = string.IsNullOrWhiteSpace(typed.Message)
                    ? fallbackMessage
                    : typed.Message;

                return typed;
            }
        }
        catch
        {
            // Если не смогли распарсить AuthApiResponse, пробуем извлечь сообщение ниже.
        }

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        var message = TryExtractProblemMessage(raw) ?? fallbackMessage;
        if ((int)response.StatusCode == 429)
        {
            message = "Слишком много попыток. Подождите минуту и попробуйте снова.";
        }

        return new AuthApiResponse
        {
            IsSuccess = response.IsSuccessStatusCode,
            Message = message
        };
    }

    private static string? TryExtractProblemMessage(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            if (root.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
            {
                return msg.GetString();
            }

            if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
            {
                return title.GetString();
            }
        }
        catch
        {
            // ignore parsing errors
        }

        return null;
    }
}

