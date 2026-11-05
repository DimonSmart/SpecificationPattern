using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace DimonSmart.Specification.EntityFrameworkCore;

/// <summary>
/// Applies EF Core specifications to an existing EF Core IQueryable without
/// replacing its query root or discarding earlier query operators.
/// </summary>
public static class EFCoreQueryableSpecificationExtension
{
    public static IQueryable<T> BySpecification<T, TState>(
        this IQueryable<T> query, EFCoreSpecification<T, TState> specification)
        where T : class
        where TState : SpecificationState
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(specification);
        EnsureEfCoreProvider(query);
        return Apply(query, specification.Core.Data, specification.Options);
    }

    /// <summary>Applies the entity query configuration before the terminal projection.</summary>
    public static IQueryable<TResult> BySpecification<TEntity, TResult>(
        this IQueryable<TEntity> query,
        EFCoreProjectedSpecification<TEntity, TResult> specification)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(specification);
        EnsureEfCoreProvider(query);
        return Apply(query, specification.Projection.SourceData, specification.Options)
            .Select(specification.Projection.Selector);
    }

    private static void EnsureEfCoreProvider<T>(IQueryable<T> query)
    {
        if (query.Provider is not IAsyncQueryProvider)
            throw new ArgumentException(
                "An EF Core IQueryable source is required to apply EFCoreSpecification. " +
                "Use the base Specification<T, TState> for LINQ-to-Objects sources.", nameof(query));
    }

    private static IQueryable<TEntity> Apply<TEntity>(
        IQueryable<TEntity> query,
        SpecificationData<TEntity> sourceData,
        QueryOptions<TEntity> options)
        where TEntity : class
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
        query = options.QueryFilters.Mode switch
        {
            QueryFilterMode.All => query.IgnoreQueryFilters(),
            QueryFilterMode.Named => query.IgnoreQueryFilters(options.QueryFilters.Names),
            _ => query
        };

        query = options.Splitting switch
        {
            QuerySplittingMode.Single => query.AsSingleQuery(),
            QuerySplittingMode.Split => query.AsSplitQuery(),
            _ => query
        };

        foreach (var tag in options.Tags)
            query = query.TagWith(tag);

        return SpecificationExtension.Apply(query, sourceData);
    }
}
