using RatingService.Core.Entities;
using RatingService.Shared.Dtos;

namespace RatingService.Core.Interfaces;

public interface IReviewService
{
    Task<ProductReview> CreateProductReview(CreateReviewDto reviewDto);
    Task<bool> UpdateProductReview(Guid productId, UpdateReviewDto reviewDto);
    Task<bool> DeleteProductReview(Guid reviewId);

    Task<IEnumerable<ProductReview>> GetReviewsByProductId(Guid productId, int pageNumber, int pageSize);
    Task<IEnumerable<ProductReview>> GetProductReviewsByRating(Guid productId, int rating, int page, int pageSize);
    Task<IEnumerable<ProductReview>> GetReviewsByDate(DateTime date, int pageNumber, int pageSize);
    Task<IEnumerable<ProductReview>> GetAllReviews(int pageNumber, int pageSize);
    Task<ProductReview> GetReviewByid(Guid reviewId);
}