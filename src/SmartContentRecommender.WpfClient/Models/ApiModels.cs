using System.Text.Json.Serialization;
using System.Windows.Media;

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

public class CategoryItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TagItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CreateContentRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Url { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public List<Guid> TagIds { get; set; } = [];
}

public class UpdateContentRequest : CreateContentRequest
{
}

public class DbOverview
{
    public int Users { get; set; }
    public int Categories { get; set; }
    public int Tags { get; set; }
    public int Contents { get; set; }
    public int Actions { get; set; }
}

public class DbUserRow
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class DbContentRow
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public class DbActionRow
{
    public Guid Id { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string ContentTitle { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public class RecommendationExplanationItem
{
    public Guid ContentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public double FinalScore { get; set; }
    public double PopularityFactor { get; set; }
    public double CategoryAffinityFactor { get; set; }
    public double KnnSimilarityFactor { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class InterestProfileItem
{
    public string Name { get; set; } = string.Empty;
    public double Score { get; set; }
}

public class InterestProfile
{
    public Guid UserId { get; set; }
    public int TotalActions { get; set; }
    public List<InterestProfileItem> TopCategories { get; set; } = [];
    public List<InterestProfileItem> TopTags { get; set; } = [];
}

public class RecommendationMetricsItem
{
    public Guid UserId { get; set; }
    public string Algorithm { get; set; } = string.Empty;
    public int K { get; set; }
    public double PrecisionAtK { get; set; }
    public double RecallAtK { get; set; }
    public double NdcgAtK { get; set; }
}

public class ChartBarItem
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public double Percent { get; set; }
    public Brush Brush { get; set; } = Brushes.SteelBlue;
}

