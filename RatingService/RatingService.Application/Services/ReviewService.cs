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
    IReviewAggregateRepository reviewAggregateRepository,
    IReviewImageService imageService,
    IMapper mapper
    ) : IReviewService
{

    public async Task<ProductReview> CreateProductReview(CreateReviewDto reviewDto)
    {
        await unitOfWork.BeginTransactionAsync();

        try
        {
            // 1. Маппинг отзыва
            var review = mapper.Map<ProductReview>(reviewDto);

            // 2. Загрузка изображений
            if (reviewDto.Images is { Length: > 0 })
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
            {
                review.ImageUrls = Array.Empty<string>();
            }

            // 3. Получаем или создаём Aggregate
            var aggregate =
                await reviewAggregateRepository.GetReviewAggregateByProductIdAsync(review.ProductId)
                ?? new ProductReviewAggregate
                {
                    ProductId = review.ProductId,
                    ReviewCount = 0,
                    AverageRating = 0,
                    BayesianRating = 0
                };

            // 4. Обновляем aggregate
            var oldCount = aggregate.ReviewCount;
            aggregate.ReviewCount++;

            var totalRating =
                (aggregate.AverageRating * oldCount) + review.Rating;

            aggregate.AverageRating =
                totalRating / aggregate.ReviewCount;

            // 5. Bayesian rating
            const int M = 3;      // минимальное доверие
            const double C = 4.0; // baseline рейтинга маркетплейса

            aggregate.BayesianRating =
                (aggregate.ReviewCount * aggregate.AverageRating + M * C)
                / (aggregate.ReviewCount + M);

            // 6. Сохраняем
            await productReviewRepository.InsertAsync(review);

            if (oldCount == 0)
                await reviewAggregateRepository.InsertAsync(aggregate);
            else
                reviewAggregateRepository.UpdateAsync(aggregate);

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitTransactionAsync();

            return review;
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
    


    public async Task<bool> UpdateProductReview(UpdateReviewDto reviewDto)
    {
        await unitOfWork.BeginTransactionAsync();

        try
        {
            var review = await productReviewRepository.GetReviewByIdAsync(reviewDto.ReviewId);
            if (review == null)
                throw new KeyNotFoundException($"Review {reviewDto.ReviewId} not found!");

            var aggregate =
                await reviewAggregateRepository.GetReviewAggregateByProductIdAsync(review.ProductId)
                ?? throw new InvalidOperationException("Aggregate not found");

            var oldRating = review.Rating;

            // ----------- Update fields -----------
            if (reviewDto.Rating.HasValue)
                review.Rating = reviewDto.Rating.Value;

            if (reviewDto.IsAnonymous.HasValue)
                review.IsAnonymous = reviewDto.IsAnonymous.Value;

            if (reviewDto.Title != null)
                review.Title = reviewDto.Title;

            if (reviewDto.Text != null)
                review.Text = reviewDto.Text;

            // ----------- Images -----------
            var oldImageUrls = review.ImageUrls?.ToList() ?? new List<string>();
            var existingImageUrls = oldImageUrls.ToList();

            if (reviewDto.RemoveImageUrls?.Any() == true)
            {
                existingImageUrls =
                    existingImageUrls
                        .Where(x => !reviewDto.RemoveImageUrls.Contains(x))
                        .ToList();
            }

            if (reviewDto.NewImages?.Length > 0)
            {
                foreach (var image in reviewDto.NewImages)
                {
                    var url = await imageService.UploadImageAsync(image);
                    existingImageUrls.Add(url);
                }
            }

            foreach (var url in oldImageUrls.Except(existingImageUrls))
                await imageService.DeleteImageAsync(url);

            review.ImageUrls = existingImageUrls.ToArray();
            review.UpdatedAt = DateTime.UtcNow;

            // ----------- Aggregate recalculation (ONLY if rating changed) -----------
            if (reviewDto.Rating.HasValue && reviewDto.Rating.Value != oldRating)
            {
                var totalRating =
                    aggregate.AverageRating * aggregate.ReviewCount
                    - oldRating
                    + review.Rating;

                aggregate.AverageRating = totalRating / aggregate.ReviewCount;

                const int M = 5;
                const double C = 3.5;

                aggregate.BayesianRating =
                    (aggregate.ReviewCount * aggregate.AverageRating + M * C)
                    / (aggregate.ReviewCount + M);

                reviewAggregateRepository.UpdateAsync(aggregate);
            }

            productReviewRepository.Update(review);

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitTransactionAsync();

            return true;
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }



    public async Task<bool> DeleteProductReview(Guid reviewId)
    {
        await unitOfWork.BeginTransactionAsync();

        try
        {
            var review = await productReviewRepository.GetReviewByIdAsync(reviewId);
            if (review == null)
                throw new KeyNotFoundException($"Review {reviewId} not found!");

            var aggregate =
                await reviewAggregateRepository.GetReviewAggregateByProductIdAsync(review.ProductId)
                ?? throw new InvalidOperationException("Aggregate not found");

            // ----------- Delete images -----------
            if (review.ImageUrls != null)
            {
                foreach (var imageUrl in review.ImageUrls)
                    await imageService.DeleteImageAsync(imageUrl);
            }

            // ----------- Aggregate recalculation -----------
            var oldCount = aggregate.ReviewCount;
            aggregate.ReviewCount--;

            if (aggregate.ReviewCount <= 0)
            {
                aggregate.ReviewCount = 0;
                aggregate.AverageRating = 0;
                aggregate.BayesianRating = 0;
            }
            else
            {
                var totalRating =
                    aggregate.AverageRating * oldCount
                    - review.Rating;

                aggregate.AverageRating =
                    totalRating / aggregate.ReviewCount;

                const int M = 5;
                const double C = 3.5;

                aggregate.BayesianRating =
                    (aggregate.ReviewCount * aggregate.AverageRating + M * C)
                    / (aggregate.ReviewCount + M);
            }

            productReviewRepository.Delete(review);
            reviewAggregateRepository.UpdateAsync(aggregate);

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitTransactionAsync();

            return true;
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync();
            throw;
        }
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
    
    
    

    public async Task<ProductReviewAggregate> GetReviewAggregateByProductId(Guid productId)
    {
        var aggregateReview = await reviewAggregateRepository.GetReviewAggregateByProductIdAsync(productId);
        if (aggregateReview == null)
            throw new KeyNotFoundException($"Aggregate review with productId - {productId} not found!");

        return aggregateReview;
    }
}