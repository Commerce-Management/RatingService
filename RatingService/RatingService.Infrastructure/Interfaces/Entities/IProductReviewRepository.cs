using RatingService.Core.Entities;
using RatingService.Infrastructure.Interfaces.Base;

namespace RatingService.Infrastructure.Interfaces.Entities;

public interface IProductReviewRepository : IRepository<ProductReview>
{
    public Task<IEnumerable<ProductReview>> GetAllProductReviewsByIdAsync(Guid productId, int page, int pageSize);
    public Task<IEnumerable<ProductReview>> GetAllReviewsAsync(int page, int pageSize);
    public Task<ProductReview?> GetReviewByIdAsync(Guid reviewId);
    public Task<IEnumerable<ProductReview>> GetProductReviewsByRatingAsync(Guid productId, int rating, int page, int pageSize);
    public Task<IEnumerable<ProductReview>> GetReviewsByUserIdAsync(Guid userId, int page, int pageSize);
    public Task<IEnumerable<ProductReview>> GetReviewsByUserIdAndRatingAsync(Guid userId, int minRating, int maxRating, int page, int pageSize);

    public Task<IEnumerable<ProductReview>> GetReviewsByDateAsync(DateTime date, int page, int pageSize);
    
    public Task<ProductReview?> GetReviewByUserIdAndProductIdAsync(Guid userId, Guid productId);

}