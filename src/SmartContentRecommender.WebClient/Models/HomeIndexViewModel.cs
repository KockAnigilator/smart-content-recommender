namespace SmartContentRecommender.WebClient.Models;

public class HomeIndexViewModel
{
    public bool IsAuthenticated { get; set; }
    public string Role { get; set; } = "Guest";

    public string? Error { get; set; }
    public string? Info { get; set; }

    public bool ApiOnline { get; set; }

    public List<ContentItem> Contents { get; set; } = [];
    public List<RecommendationItem> Popular { get; set; } = [];
    public List<RecommendationItem> ByCategories { get; set; } = [];
    public List<RecommendationItem> Knn { get; set; } = [];

    public List<AdminUserItem> AdminUsers { get; set; } = [];
}

