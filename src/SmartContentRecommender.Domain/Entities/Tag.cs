using SmartContentRecommender.Domain.Common;

namespace SmartContentRecommender.Domain.Entities;

public class Tag : EntityBase
{
    public string Name { get; set; } = string.Empty;

    public List<ContentTag> ContentTags { get; set; } = new();
}

