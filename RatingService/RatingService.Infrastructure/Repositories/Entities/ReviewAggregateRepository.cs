using Microsoft.EntityFrameworkCore;
using RatingService.Core.Entities;
using RatingService.Infrastructure.Context;
using RatingService.Infrastructure.Interfaces.Entities;
using RatingService.Infrastructure.Repositories.Base;

namespace RatingService.Infrastructure.Repositories.Entities;

public class ReviewAggregateRepository(RatingDbContext context) : Repository<ProductReviewAggregate>(context), IReviewAggregateRepository
{
    private const int BayesianMinimumReviews  = 5;

    public async Task<ProductReviewAggregate?> GetReviewAggregateByProductIdAsync(Guid productId)
    {
        return await Entities.AsNoTracking().SingleOrDefaultAsync(agr => agr.ProductId == productId);
    }

    public async Task<ProductReviewAggregate?> RecalculateAggregateByProductIdAsync(Guid productId)
    {
        var reviews = await productReviewRepository
            .GetAll()
            .Where(r => r.ProductId == productId)
            .ToListAsync();

        var reviewCount = reviews.Count;

        var averageRating = reviewCount == 0
            ? 0
            : reviews.Average(r => r.Rating);

        // Global average по всей системе
        var globalAverage = await productReviewRepository
            .GetAll()
            .AverageAsync(r => (double?)r.Rating) ?? 0;

        var bayesianRating = reviewCount == 0
            ? globalAverage
            : (reviewCount * averageRating + BayesianMinimumReviews * globalAverage)
              / (reviewCount + BayesianMinimumReviews);

        var aggregate = await aggregateRepository.GetByProductIdAsync(productId);

        if (aggregate == null)
        {
            aggregate = new ProductReviewAggregate
            {
                ProductId = productId,
                ReviewCount = reviewCount,
                AverageRating = averageRating,
                BayesianRating = bayesianRating
            };

            await aggregateRepository.InsertAsync(aggregate);
        }
        else
        {
            aggregate.ReviewCount = reviewCount;
            aggregate.AverageRating = averageRating;
            aggregate.BayesianRating = bayesianRating;

            aggregateRepository.Update(aggregate);
        }
    }
}