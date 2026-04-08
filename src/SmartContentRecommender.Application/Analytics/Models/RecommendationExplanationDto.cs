namespace SmartContentRecommender.Application.Analytics.Models;

public class RecommendationExplanationDto
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

