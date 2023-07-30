namespace DimonSmart.Specification;

public static class SpecificationExtension
{
    // Extension method to make the call shorter and provide a fluent API
    public static IQueryable<T> BySpecification<T, TSpecification>(this IQueryable<T> query, TSpecification specification)
        where TSpecification : IBaseSpecification<T, TSpecification> where T:class
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
