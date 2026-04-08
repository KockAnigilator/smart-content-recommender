namespace SmartContentRecommender.WebClient.Models;

public class ContentItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Url { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
}

public class UserActionLogPayload
{
    public Guid ContentId { get; set; }
    public int Type { get; set; }
}

