using SmartContentRecommender.Domain.Common;

namespace SmartContentRecommender.Domain.Entities;

public class Category : EntityBase
{
    public string Name { get; set; } = string.Empty;

    public List<Content> Contents { get; set; } = new();
}

