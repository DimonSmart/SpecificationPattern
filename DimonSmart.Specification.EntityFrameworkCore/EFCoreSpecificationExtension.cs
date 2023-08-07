using Microsoft.EntityFrameworkCore;

namespace DimonSmart.Specification.EntityFrameworkCore;

public static class EFCoreSpecificationExtension
{
    public static IQueryable<T> BySpecification<T>(this DbContext context, IEFCoreSpecification<T> efCoreSpecification)
        where T : class
    {
        var query = context.Set<T>().AsQueryable<T>();

        foreach (var include in efCoreSpecification.GetIncludes())
        {
            query = query.Include(include);
        }

        if (efCoreSpecification.IsAsNoTracking)
        {
            query = query.AsNoTracking();
        }

        if (efCoreSpecification.IsIgnoreAutoIncludes)
        {
            query = query.IgnoreAutoIncludes();
        }

        return query.BySpecification(efCoreSpecification);
    }
}