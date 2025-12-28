using System.Linq.Expressions;

namespace RatingService.Infrastructure.Models;

public class QueryParameters<T>
{
    public Expression<Func<T, bool>>? Filter { get; set; }
    public Func<IQueryable<T>, IQueryable<T>>? Include { get; set; }
}