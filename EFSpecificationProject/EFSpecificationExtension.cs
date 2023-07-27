using Microsoft.EntityFrameworkCore;
using SpecificationPattern;

namespace EFSpecificationProject
{

    public static class EFSpecificationExtension
    {
        public static IQueryable<T> BySpecification<T>(this DbContext context, IEFSpecification<T> efSpecification) where T : class
        {
            var query = context.Set<T>().AsQueryable<T>();

            foreach (var include in efSpecification.GetIncludes())
            {
                query = query.Include(include);
            }

            if (efSpecification.IsAsNoTracking)
            {
                query = query.AsNoTracking();
            }

            if (efSpecification.IsIgnoreAutoIncludes)
            {
                query = query.IgnoreAutoIncludes();
            }

            return SpecificationExtension.BySpecification(query, efSpecification);
        }
    }
}