using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace DimonSmart.Specification.EntityFrameworkCore;

/// <summary>Executes EF Core pipelines against an existing query root without materializing results.</summary>
public static class EFCoreQueryableSpecificationExtension
{
    public static IQueryable<T> BySpecification<T>(
        this IQueryable<T> query, EFCoreSpecification<T> specification) where T : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(specification);
        EnsureEfCoreProvider(query);
        return Apply(query, specification.Core.Pipeline, specification.Options);
    }

    public static IQueryable<T> BySpecificationCriteria<T>(
        this IQueryable<T> query, EFCoreSpecification<T> specification) where T : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(specification);
        EnsureEfCoreProvider(query);
        return specification.Core.Pipeline.Apply(
            ApplyQueryFilters(query, specification.Options.QueryFilters), true);
    }

    public static IQueryable<TCurrent> BySpecification<TSource, TCurrent>(
        this IQueryable<TSource> query, EFCoreProjectedSpecification<TSource, TCurrent> specification)
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(specification);
        EnsureEfCoreProvider(query);
        return Apply(query, specification.Projection.Pipeline, specification.Options);
    }

    public static IQueryable<TCurrent> BySpecificationCriteria<TSource, TCurrent>(
        this IQueryable<TSource> query, EFCoreProjectedSpecification<TSource, TCurrent> specification)
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(specification);
        EnsureEfCoreProvider(query);
        return specification.Projection.Pipeline.Apply(
            ApplyQueryFilters(query, specification.Options.QueryFilters), true);
    }

    private static void EnsureEfCoreProvider<T>(IQueryable<T> query)
    {
        if (query.Provider is not IAsyncQueryProvider)
            throw new ArgumentException(
                "An EF Core IQueryable source is required to apply EFCoreSpecification. " +
                "Use the base Specification<T> for LINQ-to-Objects sources.", nameof(query));
    }

    private static IQueryable<TEntity> ApplyQueryFilters<TEntity>(
        IQueryable<TEntity> query, QueryFilterOptions queryFilters) where TEntity : class =>
        queryFilters.Mode switch
        {
            QueryFilterMode.All => query.IgnoreQueryFilters(),
            QueryFilterMode.Named => query.IgnoreQueryFilters(queryFilters.Names),
            _ => query
        };

    private static IQueryable<TCurrent> Apply<TSource, TCurrent>(
        IQueryable<TSource> query,
        QueryPipeline<TSource, TCurrent> pipeline,
        QueryOptions<TSource> options) where TSource : class
    {
        foreach (var include in options.Includes)
            query = include.Apply(query);

        query = options.Tracking switch
        {
            TrackingMode.Tracking => query.AsTracking(),
            TrackingMode.NoTracking => query.AsNoTracking(),
            TrackingMode.NoTrackingWithIdentityResolution => query.AsNoTrackingWithIdentityResolution(),
            _ => query
        };

        if (options.IgnoreAutoIncludes)
            query = query.IgnoreAutoIncludes();
        query = ApplyQueryFilters(query, options.QueryFilters);

        query = options.Splitting switch
        {
            QuerySplittingMode.Single => query.AsSingleQuery(),
            QuerySplittingMode.Split => query.AsSplitQuery(),
            _ => query
        };

        foreach (var tag in options.Tags)
            query = query.TagWith(tag);

        return pipeline.Apply(query, false);
    }
}
