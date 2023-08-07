namespace DimonSmart.Specification;

public static class SpecificationExtension
{
    public static IQueryable<T> BySpecification<T, TSpecification>(this IQueryable<T> query,
        TSpecification specification)
        where TSpecification : IBaseSpecification<T, TSpecification> where T : class
    {
        var specData = specification.SpecificationData;
        if (specData.WhereExpression != null)
        {
            query = query.Where(specData.WhereExpression);
        }

        if (specData.OrderExpressions.Any())
        {
            var (direction, expr) = specData.OrderExpressions.First();
            var orderedQuery = direction ? query.OrderBy(expr) : query.OrderByDescending(expr);
            var thenOrderBy = specData.OrderExpressions.Skip(1);
            query = thenOrderBy.Aggregate(orderedQuery,
                (current, orderBy) => orderBy.direction
                    ? orderedQuery.ThenBy(orderBy.expr)
                    : orderedQuery.ThenByDescending(orderBy.expr));
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