namespace DimonSmart.Specification;

public static class SpecificationExtension
{
    public static IQueryable<T> BySpecification<T, TState>(this IQueryable<T> query,
        Specification<T, TState> specification)
        where T : class
        where TState : SpecificationState
    {
        ArgumentNullException.ThrowIfNull(specification);
        return Apply(query, specification.Data);
    }

    /// <summary>Applies source query operations before projecting to TResult.</summary>
    public static IQueryable<TResult> BySpecification<TEntity, TResult>(
        this IQueryable<TEntity> query,
        ProjectedSpecification<TEntity, TResult> specification)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(specification);
        return Apply(query, specification.SourceData).Select(specification.Selector);
    }

    internal static IQueryable<T> Apply<T>(IQueryable<T> query, SpecificationData<T> specData)
        where T : class
    {
        if (specData.WhereExpression != null)
        {
            query = query.Where(specData.WhereExpression);
        }

        var clauses = specData.OrderClauses;
        if (specData.Seek is { } boundary)
            query = query.Where(SeekPredicateBuilder.Build(clauses, boundary));
        if (clauses.Count > 0)
        {
            IOrderedQueryable<T> ordered = clauses[0].ApplyFirst(query);
            for (var i = 1; i < clauses.Count; i++)
                ordered = clauses[i].ApplyNext(ordered);
            query = ordered;
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