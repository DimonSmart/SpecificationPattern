namespace DimonSmart.Specification;

public static class SpecificationExtension
{
    /// <summary>Applies a specification to an in-memory or other IEnumerable source.</summary>
    public static IEnumerable<T> BySpecification<T>(
        this IEnumerable<T> source,
        Specification<T> specification)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(specification);
        return source.AsQueryable().BySpecification(specification);
    }

    /// <summary>
    /// Applies only the specification's Where criteria to an IEnumerable source.
    /// Sorting, paging and keyset boundaries configured in the specification are ignored.
    /// Enumeration remains deferred.
    /// </summary>
    public static IEnumerable<T> BySpecificationCriteria<T>(
        this IEnumerable<T> source,
        Specification<T> specification)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(specification);
        return source.AsQueryable().BySpecificationCriteria(specification);
    }

    /// <summary>Applies a projected specification to an IEnumerable source.</summary>
    public static IEnumerable<TResult> BySpecification<TEntity, TResult>(
        this IEnumerable<TEntity> source,
        ProjectedSpecification<TEntity, TResult> specification)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(specification);
        return source.AsQueryable().BySpecification(specification);
    }

    public static IQueryable<T> BySpecification<T>(this IQueryable<T> query,
        Specification<T> specification)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(specification);
        return Apply(query, specification.Data);
    }

    /// <summary>
    /// Applies only the specification's Where criteria, preserving all existing source query operations.
    /// The specification's ordering, paging and SeekAfter settings are ignored.
    /// </summary>
    public static IQueryable<T> BySpecificationCriteria<T>(
        this IQueryable<T> query,
        Specification<T> specification)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(specification);
        return ApplyCriteria(query, specification.Data);
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

    internal static IQueryable<T> ApplyCriteria<T>(IQueryable<T> query, SpecificationData<T> specData)
        where T : class =>
        specData.WhereExpression is { } predicate ? query.Where(predicate) : query;

    internal static IQueryable<T> Apply<T>(IQueryable<T> query, SpecificationData<T> specData)
        where T : class
    {
        query = ApplyCriteria(query, specData);

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