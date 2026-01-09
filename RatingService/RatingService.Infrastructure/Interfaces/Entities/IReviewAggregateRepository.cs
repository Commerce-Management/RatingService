using RatingService.Core.Entities;
using RatingService.Infrastructure.Interfaces.Base;

namespace RatingService.Infrastructure.Interfaces.Entities;

public interface IReviewAggregateRepository
{
    Task<ProductReviewAggregate?> GetReviewAggregateByProductIdAsync(Guid productId);
    Task InsertAsync(ProductReviewAggregate aggregate);
    void UpdateAsync(ProductReviewAggregate aggregate);
}