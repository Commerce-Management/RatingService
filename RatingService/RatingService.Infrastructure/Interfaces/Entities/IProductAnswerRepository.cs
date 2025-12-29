using RatingService.Core.Entities;
using RatingService.Infrastructure.Interfaces.Base;

namespace RatingService.Infrastructure.Interfaces.Entities;

public interface IProductAnswerRepository : IRepository<ProductAnswer>
{
    public Task<ProductAnswer?> GetAnswerByIdAsync(Guid answerId);
}