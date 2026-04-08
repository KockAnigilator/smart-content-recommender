namespace SmartContentRecommender.Application.Recommendations.Models;

public class RecommendationMetricsDto
{
    public Guid UserId { get; set; }
    public string Algorithm { get; set; } = string.Empty;
    public int K { get; set; }

    public double PrecisionAtK { get; set; }
    public double RecallAtK { get; set; }
    public double NdcgAtK { get; set; }
}

