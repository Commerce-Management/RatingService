using RatingService.Core.Entities;
using RatingService.Infrastructure.Interfaces.Base;

namespace RatingService.Infrastructure.Interfaces.Entities;

public interface IReviewAggregateRepository  : IRepository<ProductReviewAggregate>
{
    public Task<ProductReviewAggregate?> GetReviewAggregateByProductIdAsync(Guid productId);
    public Task<ProductReviewAggregate?> RecalculateAggregateByProductIdAsync(Guid productId);
}