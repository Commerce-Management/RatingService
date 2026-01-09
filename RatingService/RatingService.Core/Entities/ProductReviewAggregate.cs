namespace RatingService.Core.Entities;

public class ProductReviewAggregate
{
    public Guid ProductId { get; set; }

    public int ReviewCount { get; set; }
    public double AverageRating { get; set; }      // simple average
    public double BayesianRating { get; set; }     // weighted score
}