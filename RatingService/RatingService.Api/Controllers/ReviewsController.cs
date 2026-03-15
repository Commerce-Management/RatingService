using System.Security.Claims;
using Asp.Versioning;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RatingService.Application.Services;
using RatingService.Core.Interfaces;
using RatingService.Shared.Dtos;
using Serilog;

namespace RatingService.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class ReviewsController(IReviewService reviewService) : ControllerBase
{
    // POST: api/v1/reviews
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateReview([FromForm] CreateReviewDto reviewDto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(kvp => kvp.Value.Errors.Any())
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            Log.Warning("ReviewsController.CreateReview: Validation failed {@Errors}", errors);

            return BadRequest(new
            {
                ErrorCode = "ValidationError",
                Message = "Некорректные входные данные.",
                Details = errors
            });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            Log.Error("ReviewsController.CreateReview: Invalid user claim. Claims: {@Claims}",
                User.Claims.Select(c => new { c.Type, c.Value }));
            return Unauthorized(new
            {
                ErrorCode = "UserIdentificationFailed",
                Message = "Не удалось определить пользователя из токена."
            });
        }

        try
        {
            Log.Information("ReviewsController.CreateReview: Creating review {@Dto}", reviewDto);

            var review = await reviewService.CreateProductReview(reviewDto);

            return CreatedAtAction(
                nameof(GetReviewById),
                new { reviewId = review.Id },
                review
            );
        }
        catch (RpcException rpcEx)
        {
            Log.Error(rpcEx,
                "gRPC error: StatusCode={StatusCode}, Detail={Detail}, InnerException={InnerException}",
                rpcEx.StatusCode,
                rpcEx.Status.Detail,
                rpcEx.InnerException?.Message);

            return StatusCode(500, new
            {
                ErrorCode = "GrpcError",
                Message = rpcEx.Status.Detail,
                StatusCode = rpcEx.StatusCode.ToString(),
                InnerException = rpcEx.InnerException?.Message,
                StackTrace = rpcEx.StackTrace
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ReviewsController.CreateReview: Unexpected error {@Dto}", reviewDto);

            return StatusCode(500, new
            {
                ErrorCode = "InternalServerError",
                Message = "Ошибка при создании отзыва.",
                ExceptionType = ex.GetType().Name,
                ExceptionMessage = ex.Message,
                InnerException = ex.InnerException?.Message,
                StackTrace = ex.StackTrace
            });
        }
    }

    // GET: api/v1/reviews/{reviewId}
    [HttpGet("{reviewId:guid}")]
    public async Task<IActionResult> GetReviewById(Guid reviewId)
    {
        try
        {
            var review = await reviewService.GetReviewByid(reviewId);
            return Ok(review);
        }
        catch (KeyNotFoundException ex)
        {
            Log.Warning(ex, "ReviewsController.GetReviewById: Review {ReviewId} not found", reviewId);
            return NotFound(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ReviewsController.GetReviewById: Error fetching review {ReviewId}", reviewId);
            return StatusCode(500, new { Error = "Server error." });
        }
    }

    // PUT: api/v1/reviews/{reviewId}
    [Authorize]
    [HttpPut]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateReview([FromForm] UpdateReviewDto reviewDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Error = "Invalid data." });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
            return Unauthorized(new { Error = "Cannot determine user." });

        try
        {
            var updated = await reviewService.UpdateProductReview(reviewDto);
            return updated ? NoContent() : NotFound();
        }
        catch (KeyNotFoundException ex)
        {
            Log.Warning(ex, "ReviewsController.UpdateReview: Review {ReviewId} not found", reviewDto.ReviewId);
            return NotFound(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ReviewsController.UpdateReview: Error updating review {ReviewId}", reviewDto.ReviewId);

            return StatusCode(500, new
            {
                Error = "Server error.",
                ExceptionMessage = ex.Message,
                ExceptionType = ex.GetType().Name,
                StackTrace = ex.StackTrace
            });
        }
    }

    // DELETE: api/v1/reviews/{reviewId}
    [HttpDelete("{reviewId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteReview(Guid reviewId)
    {
        try
        {
            var deleted = await reviewService.DeleteProductReview(reviewId);
            return deleted ? NoContent() : NotFound();
        }
        catch (KeyNotFoundException ex)
        {
            Log.Warning(ex, "ReviewsController.DeleteReview: Review {ReviewId} not found", reviewId);
            return NotFound(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ReviewsController.DeleteReview: Error deleting review {ReviewId}", reviewId);
            return StatusCode(500, new { Error = "Server error." });
        }
    }

    // GET: api/v1/reviews/product/{productId}
    [HttpGet("product/{productId:guid}")]
    public async Task<IActionResult> GetReviewsByProductId(
        Guid productId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        if (pageNumber < 1 || pageSize < 1)
            return BadRequest(new { Error = "PageNumber and PageSize must be greater than 0." });

        try
        {
            var reviews = await reviewService
                .GetReviewsByProductId(productId, pageNumber, pageSize);

            if (!reviews.Any())
                return NotFound(new { Error = "No reviews found." });

            return Ok(reviews);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ReviewsController.GetReviewsByProductId: Error for product {ProductId}", productId);
            return StatusCode(500, new { Error = "Server error." });
        }
    }

    // GET: api/v1/reviews/user/{userId}
    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetReviewsByUserId(
        Guid userId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        if (pageNumber < 1 || pageSize < 1)
            return BadRequest(new { Error = "PageNumber and PageSize must be greater than 0." });

        try
        {
            var reviews = await reviewService
                .GetReviewsByUserId(userId, pageNumber, pageSize);

            if (!reviews.Any())
                return NotFound(new { Error = "No reviews found." });

            return Ok(reviews);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ReviewsController.GetReviewsByUserId: Error for user {UserId}", userId);
            return StatusCode(500, new { Error = "Server error." });
        }
    }

    // GET: api/v1/reviews/product/{productId}/rating/{rating}
    [HttpGet("product/{productId:guid}/rating/{rating:int}")]
    public async Task<IActionResult> GetReviewsByRating(
        Guid productId,
        int rating,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var reviews = await reviewService
                .GetProductReviewsByRating(productId, rating, page, pageSize);

            if (!reviews.Any())
                return NotFound(new { Error = "No reviews found." });

            return Ok(reviews);
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "ReviewsController.GetReviewsByRating: Error for product {ProductId}, rating {Rating}",
                productId, rating);

            return StatusCode(500, new { Error = "Server error." });
        }
    }

    // GET: api/v1/reviews/by-date
    [HttpGet("by-date")]
    public async Task<IActionResult> GetReviewsByDate(
        [FromQuery] DateTime date,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var reviews = await reviewService
                .GetReviewsByDate(date, pageNumber, pageSize);

            if (!reviews.Any())
                return NotFound(new { Error = "No reviews found." });

            return Ok(reviews);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ReviewsController.GetReviewsByDate: Error for date {Date}", date);
            return StatusCode(500, new { Error = "Server error." });
        }
    }

    // GET: api/v1/reviews
    [HttpGet]
    public async Task<IActionResult> GetAllReviews(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        if (pageNumber < 1 || pageSize < 1)
            return BadRequest(new { Error = "PageNumber and PageSize must be greater than 0." });

        try
        {
            var reviews = await reviewService.GetAllReviews(pageNumber, pageSize);

            if (!reviews.Any())
                return NotFound(new { Error = "No reviews found." });

            return Ok(reviews);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ReviewsController.GetAllReviews: Error fetching reviews");
            return StatusCode(500, new { Error = "Server error." });
        }
    }

    // GET: api/v1/reviews/shop/{shopId}/aggregate?from=2026-01-01&to=2026-02-01
    [HttpGet("shop/{shopId:guid}/aggregate")]
    public async Task<IActionResult> GetAggregateByShopId(
        Guid shopId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var agg = await reviewService.GetAggregateByShopIdAsync(shopId, from, to);
            return Ok(agg);
        }
        catch (KeyNotFoundException ex)
        {
            Log.Warning(ex, "Aggregate not found for shop {ShopId}", shopId);
            return NotFound(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error fetching aggregate for shop {ShopId}", shopId);
            return StatusCode(500, new { Error = "Server error." });
        }
    }



}