using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using SpecificationPattern;

namespace EFSpecificationProject
{

    public static class EFSpecificationExtension
    {
        public static IQueryable<T> BySpecification<T>(this DbContext context, IEFSpecification<T> efSpecification) where T : class
        {
            var query = context.Set<T>().AsQueryable<T>();

            foreach (var include in efSpecification.Includes)
            {
                query = query.Include(include);
            }

            return SpecificationExtension.BySpecification(query, efSpecification);
        }
    }
}