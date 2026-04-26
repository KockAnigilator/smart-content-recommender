namespace SmartContentRecommender.WebClient.Models;

public class HomeIndexViewModel
{
    public bool IsAuthenticated { get; set; }
    public string Role { get; set; } = "Guest";
    public bool IsAdmin { get; set; }

    public string? Error { get; set; }
    public string? Info { get; set; }

    public bool ApiOnline { get; set; }
    public bool ShowDemoHistoryButton { get; set; }

    public List<ContentItem> Contents { get; set; } = [];
    public List<RecommendationItem> Popular { get; set; } = [];
    public List<RecommendationItem> ByCategories { get; set; } = [];
    public List<RecommendationItem> Knn { get; set; } = [];
    public InterestProfileVm? InterestProfile { get; set; }
    public List<RecommendationExplanationVm> ExplainKnn { get; set; } = [];

    public List<AdminUserItem> AdminUsers { get; set; } = [];
    public RecommendationMetricsVm? SelectedMetrics { get; set; }
    public Guid? SelectedMetricsUserId { get; set; }
    public string SelectedMetricsAlgorithm { get; set; } = "knn";

    public List<CategoryItemVm> Categories { get; set; } = [];
    public List<TagItemVm> Tags { get; set; } = [];
    public CreateContentVm NewContent { get; set; } = new();
    public UpdateContentVm EditContent { get; set; } = new();

    public DbOverviewVm? DbOverview { get; set; }
    public List<DbUserRowVm> DbUsers { get; set; } = [];
    public List<DbCategoryRowVm> DbCategories { get; set; } = [];
    public List<DbTagRowVm> DbTags { get; set; } = [];
    public List<DbContentRowVm> DbContents { get; set; } = [];
    public List<DbActionRowVm> DbActions { get; set; } = [];
}

