using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace DimonSmart.Specification.EntityFrameworkCore;

/// <summary>Constructs an EF Core-translatable LIKE predicate without Invoke or Compile.</summary>
internal static class EFCoreSearchPredicate
{
    internal static Expression<Func<T, bool>> Build<T>(
        Expression<Func<T, string?>> selector, string pattern)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(pattern);

        var like = Expression.Call(typeof(DbFunctionsExtensions),
            nameof(DbFunctionsExtensions.Like), Type.EmptyTypes,
            Expression.Property(null, typeof(EF), nameof(EF.Functions)),
            selector.Body, Expression.Constant(pattern));

        return Expression.Lambda<Func<T, bool>>(like, selector.Parameters);
    }
}
