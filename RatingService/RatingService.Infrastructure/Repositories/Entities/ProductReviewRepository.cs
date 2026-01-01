using Microsoft.EntityFrameworkCore;
using RatingService.Core.Entities;
using RatingService.Infrastructure.Context;
using RatingService.Infrastructure.Interfaces.Entities;
using RatingService.Infrastructure.Repositories.Base;

namespace RatingService.Infrastructure.Repositories.Entities;

public class ProductReviewRepository(RatingDbContext context) : Repository<ProductReview>(context), IProductReviewRepository
{

    public async Task<IEnumerable<ProductReview>> GetAllProductReviewsByIdAsync(Guid productId, int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;

        return await Entities.Where(review => review.ProductId == productId)
            .Skip(skip)
            .Take(pageSize)
            .AsNoTracking().ToListAsync();
    }

    public async Task<IEnumerable<ProductReview>> GetAllReviewsAsync(int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;

        return await Entities
            .Skip(skip)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<ProductReview?> GetReviewByIdAsync(Guid reviewId)
    {
        return await Entities
            .AsNoTracking()
            .SingleOrDefaultAsync(review => review.Id == reviewId);
    }

    public async Task<IEnumerable<ProductReview>> GetProductReviewsByRatingAsync(Guid productId, int rating, int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;

        return await Entities
            .Where(review => review.ProductId == productId && review.Rating == rating)
            .Skip(skip)
            .Take(pageSize)
            .AsNoTracking().ToListAsync();
    }

    public async Task<IEnumerable<ProductReview>> GetReviewsByDateAsync(
        DateTime date,
        int page,
        int pageSize)
    {
        var skip = (page - 1) * pageSize;

        var start = date.Date;
        var end = start.AddDays(1);

        return await Entities
            .Where(r => r.CreatedAt >= start && r.CreatedAt < end)
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }

}