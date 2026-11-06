namespace DimonSmart.Specification;

public static class SpecificationExtension
{
    public static IEnumerable<T> BySpecification<T>(
        this IEnumerable<T> source, Specification<T> specification) where T : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(specification);
        return source.AsQueryable().BySpecification(specification);
    }

    public static IEnumerable<T> BySpecificationCriteria<T>(
        this IEnumerable<T> source, Specification<T> specification) where T : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(specification);
        return source.AsQueryable().BySpecificationCriteria(specification);
    }

    public static IEnumerable<TCurrent> BySpecification<TSource, TCurrent>(
        this IEnumerable<TSource> source, ProjectedSpecification<TSource, TCurrent> specification)
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(specification);
        return source.AsQueryable().BySpecification(specification);
    }

    public static IEnumerable<TCurrent> BySpecificationCriteria<TSource, TCurrent>(
        this IEnumerable<TSource> source, ProjectedSpecification<TSource, TCurrent> specification)
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(specification);
        return source.AsQueryable().BySpecificationCriteria(specification);
    }

    public static IQueryable<T> BySpecification<T>(
        this IQueryable<T> query, Specification<T> specification) where T : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(specification);
        return specification.Pipeline.Apply(query, false);
    }

    public static IQueryable<T> BySpecificationCriteria<T>(
        this IQueryable<T> query, Specification<T> specification) where T : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(specification);
        return specification.Pipeline.Apply(query, true);
    }

    public static IQueryable<TCurrent> BySpecification<TSource, TCurrent>(
        this IQueryable<TSource> query, ProjectedSpecification<TSource, TCurrent> specification)
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(specification);
        return specification.Pipeline.Apply(query, false);
    }

    public static IQueryable<TCurrent> BySpecificationCriteria<TSource, TCurrent>(
        this IQueryable<TSource> query, ProjectedSpecification<TSource, TCurrent> specification)
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(specification);
        return specification.Pipeline.Apply(query, true);
    }
}
