using Microsoft.EntityFrameworkCore;
using RatingService.Core.Entities;
using RatingService.Infrastructure.Context;
using RatingService.Infrastructure.Interfaces.Entities;
using RatingService.Infrastructure.Repositories.Base;

namespace RatingService.Infrastructure.Repositories.Entities;

public class ReviewAggregateRepository(RatingDbContext context, IProductReviewRepository productReviewRepository) : IReviewAggregateRepository
{

    public async Task<ProductReviewAggregate?> GetReviewAggregateByProductIdAsync(Guid productId)
    {
        return await context.ProductReviewAggregate
            .SingleOrDefaultAsync(x => x.ProductId == productId);
    }

    public async Task InsertAsync(ProductReviewAggregate aggregate)
    {
        await context.ProductReviewAggregate.AddAsync(aggregate);
    }

    public void UpdateAsync(ProductReviewAggregate aggregate)
    {
        context.ProductReviewAggregate.Update(aggregate);
    }
}
