using Microsoft.EntityFrameworkCore;

namespace DimonSmart.Specification.EntityFrameworkCore;

public static class EFCoreSpecificationExtension
{
    public static IQueryable<T> BySpecification<T, TState>(
        this DbContext context, EFCoreSpecification<T, TState> specification)
        where T : class
        where TState : SpecificationState =>
        PrepareQuery(context, specification.EFCoreSpecificationData).BySpecification(specification.Core);

    private static IQueryable<T> PrepareQuery<T>(DbContext context, IEFCoreSpecificationData<T> spec)
        where T : class
    {
        var query = context.Set<T>().AsQueryable<T>();

        foreach (var include in spec.Includes)
        {
            query = query.Include(include);
        }

        if (spec.IsAsNoTracking)
        {
            query = query.AsNoTracking();
        }

        if (spec.IsAsNoTrackingWithIdentityResolution)
        {
            query = query.AsNoTrackingWithIdentityResolution();
        }

        if (spec.IsIgnoreAutoIncludes)
        {
            query = query.IgnoreAutoIncludes();
        }

        if (spec.IsIgnoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        if (spec.IsAsSingleQuery)
        {
            query = query.AsSingleQuery();
        }

        if (spec.IsAsSplitQuery)
        {
            query = query.AsSplitQuery();
        }

        if (!string.IsNullOrWhiteSpace(spec.Tag))
        {
            query = query.TagWith(spec.Tag);
        }

        return query;
    }
}