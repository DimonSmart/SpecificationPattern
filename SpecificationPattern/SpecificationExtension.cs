namespace SpecificationPattern;

public static class SpecificationExtension
{
    public static IQueryable<T> BySpecification<T>(this IQueryable<T> query, Specification<T> specification)
    {
        if (specification.WhereExpression != null)
        {
            query = query.Where(specification.WhereExpression);
        }

        if (specification.OrderExpressions.Any())
        {
            var (direction, expr) = specification.OrderExpressions.First();
            var orderedQuery = direction ? query.OrderBy(expr) : query.OrderByDescending(expr);
            var thenOrderBy = specification.OrderExpressions.Skip(1);
            query = thenOrderBy.Aggregate(orderedQuery, (current, orderBy) => orderBy.direction ? orderedQuery.ThenBy(orderBy.expr) : orderedQuery.ThenByDescending(orderBy.expr));
        }

        if (specification.SkipQ.HasValue)
        {
            query = query.Take(specification.SkipQ.Value);
        }

        if (specification.TakeQ.HasValue)
        {
            query = query.Take(specification.TakeQ.Value);
        }

        return query;
    }
}
