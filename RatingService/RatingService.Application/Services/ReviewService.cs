using AutoMapper;
using RatingService.Core.Entities;
using RatingService.Core.Interfaces;
using RatingService.Infrastructure.Interfaces.Base;
using RatingService.Infrastructure.Interfaces.Entities;
using RatingService.Shared.Dtos;
using RatingService.Shared.Protos.GrpcOrderService;
using RatingService.Shared.Protos.GrpcProductService;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Grpc.Core;

namespace RatingService.Application.Services;

public class ReviewService(
    IUnitOfWork unitOfWork,
    IProductReviewRepository productReviewRepository,
    IReviewAggregateRepository reviewAggregateRepository,
    OrderService.OrderServiceClient orderClient,
    ProductService.ProductServiceClient productGrpc,
    IReviewImageService imageService,
    IMapper mapper
    ) : IReviewService
{

    public async Task<ProductReview> CreateProductReview(CreateReviewDto reviewDto)
    {
        await unitOfWork.BeginTransactionAsync();

        try
        {
            
          
            var orderStatusResponse = await orderClient.GetOrderStatusByProductIdAndUserIdAsync(
                new GetOrderStatusByProductIdAndUserIdRequest
                {
                    ProductId = reviewDto.ProductId.ToString(),
                    UserId = reviewDto.UserId.ToString()
                });

            if (!orderStatusResponse.Exists)
                throw new Exception("You cannot review a product you haven't purchased");

            if (!string.Equals(orderStatusResponse.Status, "Delivered", StringComparison.OrdinalIgnoreCase))
                throw new Exception("You can review a product only after it is delivered");

            var existingReview = await productReviewRepository.GetReviewByUserIdAndProductIdAsync(
                reviewDto.UserId, 
                reviewDto.ProductId);
        
            if (existingReview != null)
                throw new Exception("You have already reviewed this product");

         
            var review = mapper.Map<ProductReview>(reviewDto);

          
            try
            {
                var prodResp = await productGrpc.GetProductsByIdsAsync(new GetProductsByIdsRequest
                {
                    ProductIds = { reviewDto.ProductId.ToString() }
                });

                var brief = prodResp?.Products?.FirstOrDefault();
                if (brief != null && !string.IsNullOrWhiteSpace(brief.ShopId))
                {
                    if (Guid.TryParse(brief.ShopId, out var shopGuid))
                        review.ShopId = shopGuid;
                    else
                        review.ShopId = null;
                }
                else
                {
                   
                    review.ShopId = null;
                }
            }
            catch (RpcException ex)
            {
           
                Log.Warning(ex, "ProductService gRPC failed while fetching product info for {ProductId}", reviewDto.ProductId);
                review.ShopId = null;
            }

        
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

      
            var aggregate =
                await reviewAggregateRepository.GetReviewAggregateByProductIdAsync(review.ProductId)
                ?? new ProductReviewAggregate
                {
                    ProductId = review.ProductId,
                    ReviewCount = 0,
                    AverageRating = 0,
                    BayesianRating = 0
                };

           
            var oldCount = aggregate.ReviewCount;
            aggregate.ReviewCount++;

            var totalRating =
                (aggregate.AverageRating * oldCount) + review.Rating;

            aggregate.AverageRating =
                totalRating / aggregate.ReviewCount;

           
            const int M = 3;      
            const double C = 4.0;  

            aggregate.BayesianRating =
                (aggregate.ReviewCount * aggregate.AverageRating + M * C)
                / (aggregate.ReviewCount + M);

          
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

        
            if (reviewDto.Rating.HasValue)
                review.Rating = reviewDto.Rating.Value;

            if (reviewDto.IsAnonymous.HasValue)
                review.IsAnonymous = reviewDto.IsAnonymous.Value;

            if (reviewDto.Title != null)
                review.Title = reviewDto.Title;

            if (reviewDto.Text != null)
                review.Text = reviewDto.Text;

            
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

     
            if (review.ImageUrls != null)
            {
                foreach (var imageUrl in review.ImageUrls)
                    await imageService.DeleteImageAsync(imageUrl);
            }

      
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
        var reviews = await productReviewRepository.GetAllReviewsAsync(pageNumber, pageSize);
        return reviews;
    }

    public async Task<IEnumerable<ProductReview>> GetReviewsByUserId(Guid userId, int pageNumber, int pageSize)
    {
        var reviews = await productReviewRepository.GetReviewsByUserIdAsync(userId, pageNumber, pageSize);
        return reviews;
    }

    public async Task<IEnumerable<ProductReview>> GetReviewsByUserIdAndRating(Guid userId, int minRating, int maxRating, int pageNumber, int pageSize)
    {
        var reviews = await productReviewRepository.GetReviewsByUserIdAndRatingAsync(userId, minRating, maxRating, pageNumber, pageSize);
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



    //NEW FOR SHOP

    public async Task<ProductReviewAggregateDto> GetAggregateByShopIdAsync(Guid shopId, DateTime? from = null, DateTime? to = null)
    {
        var q = productReviewRepository.GetQueryableEntities()
            .Where(r => r.ShopId.HasValue && r.ShopId.Value == shopId);

        if (from.HasValue)
            q = q.Where(r => r.CreatedAt >= from.Value);

        if (to.HasValue)
            q = q.Where(r => r.CreatedAt <= to.Value);

        var totalCount = await q.CountAsync();

        var result = new ProductReviewAggregateDto
        {
            TotalCount = totalCount,
            AverageRating = 0,
            Histogram = new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 } }
        };

        if (totalCount == 0) return result;

        var avg = await q.AverageAsync(r => r.Rating);
        result.AverageRating = Math.Round(avg, 2);

        var groups = await q.GroupBy(r => r.Rating)
                            .Select(g => new { Rating = g.Key, Count = g.Count() })
                            .ToListAsync();

        foreach (var g in groups)
        {
            if (g.Rating >= 1 && g.Rating <= 5)
                result.Histogram[g.Rating] = g.Count;
        }

        return result;
    }

}