namespace RatingService.Core.Entities;

public class ProductQuestion : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProductId { get; set; }
    public Guid UserId { get; set; }

    public string Text { get; set; } = null!;
    public bool IsAnonymous { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<ProductAnswer> Answers { get; set; } = new List<ProductAnswer>();
}