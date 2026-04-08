namespace SmartContentRecommender.WebClient.Models;

public class InterestProfileVm
{
    public Guid UserId { get; set; }
    public int TotalActions { get; set; }
    public List<InterestItemVm> TopCategories { get; set; } = [];
    public List<InterestItemVm> TopTags { get; set; } = [];
}

public class InterestItemVm
{
    public string Name { get; set; } = string.Empty;
    public double Score { get; set; }
}

