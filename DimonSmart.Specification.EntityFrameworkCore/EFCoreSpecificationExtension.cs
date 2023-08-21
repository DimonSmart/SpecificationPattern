using Microsoft.EntityFrameworkCore;

namespace DimonSmart.Specification.EntityFrameworkCore;

public static class EFCoreSpecificationExtension
{
    public static IQueryable<T> BySpecification<T>(this DbContext context, IEFCoreSpecification<T> efCoreSpecification)
        where T : class
    {
        var spec = efCoreSpecification.EFCoreSpecificationData;

        var query = context.Set<T>().AsQueryable<T>();

        foreach (var include in efCoreSpecification.EFCoreSpecificationData.Includes)
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

        return query.BySpecification(efCoreSpecification);
    }
}