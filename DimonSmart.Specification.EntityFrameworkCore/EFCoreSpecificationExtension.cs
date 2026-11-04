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

        var options = specification.Options;
        IQueryable<T> query = context.Set<T>();

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

        return query.BySpecification(specification.Core);
    }
}
