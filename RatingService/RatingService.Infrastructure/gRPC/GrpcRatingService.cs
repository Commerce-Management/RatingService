using Grpc.Core;
using Microsoft.Extensions.Logging;
using RatingService.Infrastructure.Interfaces.Entities;
using RatingService.Shared.Protos.GrpcRatingService;

namespace RatingService.Infrastructure.gRPC;

public class GrpcRatingService : RatingService.Shared.Protos.GrpcRatingService.RatingService.RatingServiceBase
{
    private readonly IProductReviewRepository _productReviewRepository;
    private readonly ILogger _logger;


    public GrpcRatingService(IProductReviewRepository productReviewRepository, ILogger logger)
    {
        _productReviewRepository = productReviewRepository;
        _logger = logger;
    }

    public override async Task<GetReviewsByUserIdResponse> GetReviewsByUserId(
        GetReviewsByUserIdRequest request,
        ServerCallContext context)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.UserId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "UserId is required"));

        if (!Guid.TryParse(request.UserId, out var userId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "UserId invalid"));

        var reviews = await _productReviewRepository.GetReviewsByUserIdAsync(userId, request.Page, request.PageSize);
        var response = new GetReviewsByUserIdResponse();
        foreach (var review in reviews)
        {
            response.Reviews.Add(new ReviewBrief
            {
                Id = review.Id.ToString(),
                ProductId = review.ProductId.ToString(),
                UserId = review.UserId.ToString(),
                Rating = review.Rating,
                Title = review.Title ?? "",
                Text = review.Text ?? "",
                ImageUrls = { review.ImageUrls ?? Array.Empty<string>() },
                IsAnonymous = review.IsAnonymous,
                CreatedAt = review.CreatedAt.ToString("O")
            });
        }

        return response;
    }
    
    public override async Task<GetReviewsByUserIdAndRatingResponse> GetReviewsByUserIdAndRating(
        GetReviewsByUserIdAndRatingRequest request,
        ServerCallContext context)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.UserId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "UserId is required"));

        if (!Guid.TryParse(request.UserId, out var userId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "UserId invalid"));

        var reviews = await _productReviewRepository.GetReviewsByUserIdAndRatingAsync(userId, request.MinRating, request.MaxRating, request.Page, request.PageSize);
        var response = new GetReviewsByUserIdAndRatingResponse();
        foreach (var review in reviews)
        {
            response.Reviews.Add(new ReviewBrief
            {
                Id = review.Id.ToString(),
                ProductId = review.ProductId.ToString(),
                UserId = review.UserId.ToString(),
                Rating = review.Rating, 
                Title = review.Title ?? "",
                Text = review.Text ?? "",
                ImageUrls = { review.ImageUrls ?? Array.Empty<string>() },
                IsAnonymous = review.IsAnonymous,
                CreatedAt = review.CreatedAt.ToString("O")
            });
        }

        return response;
    }
}