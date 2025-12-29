using Microsoft.EntityFrameworkCore;
using RatingService.Core.Entities;
using RatingService.Infrastructure.Context;
using RatingService.Infrastructure.Interfaces.Entities;
using RatingService.Infrastructure.Repositories.Base;

namespace RatingService.Infrastructure.Repositories.Entities;

public class ProductQuestionRepository(RatingDbContext context) : Repository<ProductQuestion>(context), IProductQuestionRepository
{

    public async Task<IEnumerable<ProductQuestion>> GetAllProductQuestionsByIdAsync(Guid productId, int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;

        return await Entities
            .Where(question => question.ProductId == productId)
            .Skip(skip)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }
    
    

    public async Task<IEnumerable<ProductQuestion>> GetQuestionsAndAnswersByProductIdAsync(Guid productId, int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;

        return await Entities
            .Where(q => q.ProductId == productId)
            .Include(q => q.Answers)
            .Skip(skip)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<ProductQuestion?> GetQuestionByIdAsync(Guid questionId)
    {
        return await Entities
            .AsNoTracking()
            .SingleOrDefaultAsync(question => question.Id == questionId);
    }
}