namespace RatingService.Core.Entities;

public class ProductAnswer : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid QuestionId { get; set; }
    public Guid ShopId { get; set; }  

    public string Text { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}