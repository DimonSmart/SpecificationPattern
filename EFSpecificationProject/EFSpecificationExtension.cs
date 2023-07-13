using Microsoft.EntityFrameworkCore;
using SpecificationPattern;

namespace EFSpecificationProject
{

    public static class EFSpecificationExtension
    {
        public static IQueryable<T> BySpecification<T>(this IQueryable<T> query, EFSpecification<T> efSpecification) where T : class
        {
            foreach (var includeExpression in efSpecification.IncludeExpressions)
            {
                query = query.Include(includeExpression);
            }

            return SpecificationExtension.BySpecification(query, efSpecification);
        }
    }
}