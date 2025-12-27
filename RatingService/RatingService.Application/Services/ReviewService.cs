using AutoMapper;
using RatingService.Core.Entities;
using RatingService.Core.Interfaces;
using RatingService.Infrastructure.Interfaces.Base;
using RatingService.Infrastructure.Interfaces.Entities;
using RatingService.Shared.Dtos;

namespace RatingService.Application.Services;

public class ReviewService(
    IUnitOfWork unitOfWork,
    IProductReviewRepository productReviewRepository,
    IReviewImageService imageService,
    IMapper mapper
    ) : IReviewService
{
    public async Task<ProductReview> CreateProductReview(CreateReviewDto reviewDto)
    {
        try
        {
            var review = mapper.Map<ProductReview>(reviewDto);

            if (reviewDto.Images is { Length: > 0})
            {
                var imageUrls = new List<string>();
                foreach (var image in reviewDto.Images)
                {
                    var url = await imageService.UploadImageAsync(image);
                    imageUrls.Add(url);
                }
                review.ImageUrls = imageUrls.ToArray();
            }
            else
                review.ImageUrls = Array.Empty<string>();

            await unitOfWork.BeginTransactionAsync();

            var insertedReview = await productReviewRepository.InsertAsync(review);
            
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitTransactionAsync();


            return insertedReview;
        }
        catch (Exception)
        {
            await unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<bool> UpdateProductReview(UpdateReviewDto reviewDto)
    {
        var review = await productReviewRepository.GetReviewByIdAsync(reviewDto.ReviewId);
        if (review == null)
            throw new KeyNotFoundException($"Review {reviewDto.ReviewId} not found!");

        var oldImageUrls = review.ImageUrls?.ToList() ?? new List<string>();

        if (reviewDto.Rating.HasValue)
            review.Rating = reviewDto.Rating.Value;

        if (reviewDto.IsAnonymous.HasValue)
            review.IsAnonymous = reviewDto.IsAnonymous.Value;

        if (reviewDto.Title != null)
            review.Title = reviewDto.Title;

        if (reviewDto.Text != null)
            review.Text = reviewDto.Text;

        var existingImageUrls = review.ImageUrls != null
            ? review.ImageUrls.ToList()
            : new List<string>();

        if (reviewDto.RemoveImageUrls != null && reviewDto.RemoveImageUrls.Any())
        {
            existingImageUrls = existingImageUrls
                .Where(url => !reviewDto.RemoveImageUrls.Contains(url))
                .ToList();
        }

        if (reviewDto.NewImages != null && reviewDto.NewImages.Length > 0)
        {
            foreach (var image in reviewDto.NewImages)
            {
                var url = await imageService.UploadImageAsync(image);
                existingImageUrls.Add(url);
            }
        }

        var imagesToDelete = oldImageUrls
            .Except(existingImageUrls)
            .ToList();

        foreach (var imageUrl in imagesToDelete)
        {
            await imageService.DeleteImageAsync(imageUrl);
        }

        review.ImageUrls = existingImageUrls.ToArray();
        review.UpdatedAt = DateTime.UtcNow;

        productReviewRepository.Update(review);
        var result = await productReviewRepository.SaveChangesAsync();

        return result > 0;
    }


    public async Task<bool> DeleteProductReview(Guid reviewId)
    {
        var review = await productReviewRepository.GetReviewByIdAsync(reviewId);
        if (review == null)
            throw new KeyNotFoundException($"Review {reviewId} not found!");

        if (review.ImageUrls != null)
        {
            foreach (var imageUrl in review.ImageUrls)
            {
                await imageService.DeleteImageAsync(imageUrl);
            }
        }

        productReviewRepository.Delete(review);
        
        var result = await productReviewRepository.SaveChangesAsync();
        return result > 0;
    }

    public async Task<IEnumerable<ProductReview>> GetReviewsByProductId(Guid productId, int pageNumber, int pageSize)
    {
        var reviews = await productReviewRepository.GetAllProductReviewsByIdAsync(productId, pageNumber, pageSize);
        return reviews;
    }

    public async Task<IEnumerable<ProductReview>> GetProductReviewsByRating(Guid productId, int rating, int page, int pageSize)
    {
        var reviews = await productReviewRepository.GetProductReviewsByRatingAsync(productId, rating, page, pageSize);
        return reviews;
    }

    public async Task<IEnumerable<ProductReview>> GetReviewsByDate(DateTime date, int pageNumber, int pageSize)
    {
        var reviews = await productReviewRepository.GetReviewsByDateAsync(date, pageNumber, pageSize);
        return reviews;
    }

    public async Task<IEnumerable<ProductReview>> GetAllReviews(int pageNumber, int pageSize)
    {
        var reviews = await productReviewRepository.GetAllReviewsAsync(pageNumber, pageSize: 30);
        return reviews;
    }

    public async Task<ProductReview> GetReviewByid(Guid reviewId)
    {
        var review = await productReviewRepository.GetReviewByIdAsync(reviewId);

        if (review == null)
            throw new KeyNotFoundException($"Review {reviewId} not found!");

        return review;
    }
}