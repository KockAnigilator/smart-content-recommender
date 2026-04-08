namespace SmartContentRecommender.WebClient.Models;

public class RecommendationExplanationVm
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

public class RecommendationMetricsVm
{
    public Guid UserId { get; set; }
    public string Algorithm { get; set; } = string.Empty;
    public int K { get; set; }
    public double PrecisionAtK { get; set; }
    public double RecallAtK { get; set; }
    public double NdcgAtK { get; set; }
}

