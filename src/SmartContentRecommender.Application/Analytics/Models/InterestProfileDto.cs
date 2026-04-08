namespace SmartContentRecommender.Application.Analytics.Models;

public class InterestProfileDto
{
    public Guid UserId { get; set; }
    public int TotalActions { get; set; }
    public List<InterestItemDto> TopCategories { get; set; } = [];
    public List<InterestItemDto> TopTags { get; set; } = [];
}

public class InterestItemDto
{
    public string Name { get; set; } = string.Empty;
    public double Score { get; set; }
}

