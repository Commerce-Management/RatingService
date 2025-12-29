using System.Runtime.CompilerServices;
using RatingService.Core.Entities;
using RatingService.Infrastructure.Interfaces.Base;

namespace RatingService.Infrastructure.Interfaces.Entities;

public interface IProductQuestionRepository : IRepository<ProductQuestion>
{
    public Task<IEnumerable<ProductQuestion>> GetAllProductQuestionsByIdAsync(Guid productId, int page, int pageSize);
    public Task<IEnumerable<ProductQuestion>> GetQuestionsAndAnswersByProductIdAsync(Guid productId, int page, int pageSize);
    
    public Task<ProductQuestion?> GetQuestionByIdAsync(Guid questionId);
    
}