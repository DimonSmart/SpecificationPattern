using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using SpecificationPattern;

namespace EFSpecificationProject
{

    public static class EFSpecificationExtension
    {
        public static IQueryable<T> BySpecification<T, TP>(this IQueryable<T> query, EFSpecification<T> efSpecification) where T : class
        {
            foreach (var includeExpression in efSpecification.Includes)
            {
                // TODO:

            }

            return SpecificationExtension.BySpecification(query, efSpecification);
        }
    }
}