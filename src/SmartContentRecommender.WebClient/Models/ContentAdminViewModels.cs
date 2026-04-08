namespace SmartContentRecommender.WebClient.Models;

public class CategoryItemVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TagItemVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CreateContentVm
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Url { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string TagIdsCsv { get; set; } = string.Empty; // "guid,guid,guid"
}

public class UpdateContentVm : CreateContentVm
{
    public Guid Id { get; set; }
}

