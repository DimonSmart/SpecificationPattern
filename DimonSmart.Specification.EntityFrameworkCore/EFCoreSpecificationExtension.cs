using Microsoft.EntityFrameworkCore;

namespace DimonSmart.Specification.EntityFrameworkCore;

public static class EFCoreSpecificationExtension
{
    public static IQueryable<T> BySpecification<T, TState>(
        this DbContext context, EFCoreSpecification<T, TState> specification)
        where T : class
        where TState : SpecificationState
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(specification);

        return Apply(context, specification.Core.Data, specification.Options);
    }

    public static IQueryable<TResult> BySpecification<TEntity, TResult>(
        this DbContext context, EFCoreProjectedSpecification<TEntity, TResult> specification)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(specification);

        return Apply(context, specification.Projection.SourceData, specification.Options)
            .Select(specification.Projection.Selector);
    }

    private static IQueryable<TEntity> Apply<TEntity>(
        DbContext context,
        ISpecificationData<TEntity> sourceData,
        QueryOptions<TEntity> options)
        where TEntity : class
    {
        IQueryable<TEntity> query = context.Set<TEntity>();

        foreach (var include in options.Includes)
            query = include.Apply(query);

        query = options.Tracking switch
        {
            TrackingMode.NoTracking => query.AsNoTracking(),
            TrackingMode.NoTrackingWithIdentityResolution => query.AsNoTrackingWithIdentityResolution(),
            _ => query
        };

        if (options.IgnoreAutoIncludes)
            query = query.IgnoreAutoIncludes();
        if (options.IgnoreQueryFilters)
            query = query.IgnoreQueryFilters();

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
