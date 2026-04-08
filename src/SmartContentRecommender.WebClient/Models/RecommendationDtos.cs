namespace SmartContentRecommender.WebClient.Models;

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

