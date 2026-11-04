using static DimonSmart.Specification.OrderDirectionEnum;

namespace DimonSmart.Specification;

public static class SpecificationExtension
{
    public static IQueryable<T> BySpecification<T, TSpecification>(this IQueryable<T> query,
        TSpecification specification)
        where TSpecification : IBaseSpecification<T, TSpecification> where T : class
    {
        return Apply(query, specification.SpecificationData);
    }

    public static IQueryable<T> BySpecification<T, TState>(this IQueryable<T> query,
        Specification<T, TState> specification)
        where T : class
        where TState : SpecificationState
    {
        ArgumentNullException.ThrowIfNull(specification);
        return Apply(query, specification.Data);
    }

    private static IQueryable<T> Apply<T>(IQueryable<T> query, ISpecificationData<T> specData)
        where T : class
    {
        if (specData.WhereExpression != null)
        {
            query = query.Where(specData.WhereExpression);
        }

        if (specData.OrderExpressions.Any())
        {
            var (direction, expr) = specData.OrderExpressions.First();
            var orderedQuery = direction == Ascending ? query.OrderBy(expr) : query.OrderByDescending(expr);
            var thenOrderBy = specData.OrderExpressions.Skip(1);
            query = thenOrderBy.Aggregate(orderedQuery,
                (current, orderBy) => orderBy.direction == Ascending
                    ? current.ThenBy(orderBy.expr)
                    : current.ThenByDescending(orderBy.expr));
        }

        if (specData.SkipQ.HasValue)
        {
            query = query.Skip(specData.SkipQ.Value);
        }

        if (specData.TakeQ.HasValue)
        {
            query = query.Take(specData.TakeQ.Value);
        }

        return query;
    }
}