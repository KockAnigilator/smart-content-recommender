namespace SmartContentRecommender.Application.Content.Models;

public class ContentItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Url { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
}

public class CreateContentRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Url { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public List<Guid> TagIds { get; set; } = [];
}

public class UpdateContentRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Url { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public List<Guid> TagIds { get; set; } = [];
}

public class CategoryItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TagItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CreateNameRequest
{
    public string Name { get; set; } = string.Empty;
}

