using SmartContentRecommender.Domain.Common;

namespace SmartContentRecommender.Domain.Entities;

public class Content : EntityBase
{
    public string Title { get; set; } = string.Empty;

    // Короткое описание (аннотация)
    public string? Description { get; set; }

    // Ссылка на контент (например, URL)
    public string Url { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Категория (1 контент -> 1 категория)
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }

    // Теги (многие-ко-многим)
    public List<ContentTag> ContentTags { get; set; } = new();

    // Действия пользователей по этому контенту
    public List<UserAction> Actions { get; set; } = new();
}

