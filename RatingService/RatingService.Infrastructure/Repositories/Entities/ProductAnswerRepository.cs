using Microsoft.EntityFrameworkCore;
using RatingService.Core.Entities;
using RatingService.Infrastructure.Context;
using RatingService.Infrastructure.Interfaces.Entities;
using RatingService.Infrastructure.Repositories.Base;

namespace RatingService.Infrastructure.Repositories.Entities;

public class ProductAnswerRepository(RatingDbContext context) : Repository<ProductAnswer>(context), IProductAnswerRepository
{
    public async Task<ProductAnswer?> GetAnswerByIdAsync(Guid answerId)
    {
        return await Entities
            .AsNoTracking()
            .SingleOrDefaultAsync(answer => answer.Id == answerId);
    }
}