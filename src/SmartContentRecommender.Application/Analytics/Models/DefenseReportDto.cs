namespace SmartContentRecommender.Application.Analytics.Models;

public class DefenseReportDto
{
    public DateTime GeneratedAtUtc { get; set; }
    public int UsersCount { get; set; }
    public int ActionsCount { get; set; }
    public List<ReportUserRowDto> Users { get; set; } = [];
    public List<ReportActionRowDto> RecentActions { get; set; } = [];
    public List<ReportMetricRowDto> RecommendationMetrics { get; set; } = [];
}

public class ReportUserRowDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
    public int ActionsCount { get; set; }
}

public class ReportActionRowDto
{
    public string UserEmail { get; set; } = string.Empty;
    public string ContentTitle { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public class ReportMetricRowDto
{
    public Guid UserId { get; set; }
    public string Algorithm { get; set; } = string.Empty;
    public int K { get; set; }
    public double PrecisionAtK { get; set; }
    public double RecallAtK { get; set; }
    public double NdcgAtK { get; set; }
}

