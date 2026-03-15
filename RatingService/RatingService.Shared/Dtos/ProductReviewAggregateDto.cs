namespace RatingService.Shared.Dtos;

public class ProductReviewAggregateDto
{
    public int TotalCount { get; set; }
    public double AverageRating { get; set; }
    public Dictionary<int, int> Histogram { get; set; } = new Dictionary<int, int>
    {
        {1,0},{2,0},{3,0},{4,0},{5,0}
    };
}