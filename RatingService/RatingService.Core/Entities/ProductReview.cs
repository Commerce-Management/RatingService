namespace RatingService.Core.Entities;

public class ProductReview : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProductId { get; set; }
    public Guid UserId { get; set; }
    public Guid? ShopId { get; set; }


    public int Rating { get; set; }    
    public string? Title { get; set; }
    public string? Text { get; set; }

    public string[]? ImageUrls { get; set; } = Array.Empty<string>();

    public bool IsAnonymous { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}