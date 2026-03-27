namespace SmartContentRecommender.Domain.Entities;

// Таблица-связка для "многие-ко-многим": Content <-> Tag
public class ContentTag
{
    public Guid ContentId { get; set; }
    public Content? Content { get; set; }

    public Guid TagId { get; set; }
    public Tag? Tag { get; set; }
}

