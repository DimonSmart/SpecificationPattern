using System.Linq.Expressions;

namespace DimonSmart.Specification.EntityFrameworkCore;

public static class EFCoreIncludeSpecificationExtensions
{
    public static IEFCoreIncludeSpecification<T, TProperty> Include<T, TProperty>(
        this IEFCoreSpecification<T> specification,
        Expression<Func<T, TProperty>> includeExpression) where T : class
    {
        specification.AddInclude(GetPropertyName(includeExpression));
        return new EFCoreIncludeSpecification<T, TProperty>(specification);
    }

    public static IEFCoreIncludeSpecification<T, TProperty> ThenInclude<T, TPreviousProperty, TProperty>(
        this IEFCoreIncludeSpecification<T, IEnumerable<TPreviousProperty>> specification,
        Expression<Func<TPreviousProperty, TProperty>> thenIncludeExpression) where T : class
    {
        specification.AddInclude(GetPropertyName(thenIncludeExpression));
        return new EFCoreIncludeSpecification<T, TProperty>(specification);
    }

    public static IEFCoreIncludeSpecification<T, TProperty> ThenInclude<T, TPreviousProperty, TProperty>(
        this IEFCoreIncludeSpecification<T, TPreviousProperty> specification,
        Expression<Func<TPreviousProperty, TProperty>> thenIncludeExpression) where T : class
    {
        specification.AddInclude(GetPropertyName(thenIncludeExpression));
        return new EFCoreIncludeSpecification<T, TProperty>(specification);
    }

    public static string GetPropertyName<T, TProperty>(Expression<Func<T, TProperty>> includeExpression)
    {
        var body = includeExpression.Body as MemberExpression ?? throw new InvalidOperationException();
        var parts = new Stack<string>();
        while (body != null)
        {
            parts.Push(body.Member.Name);
            body = body.Expression as MemberExpression;
        }

        return string.Join(".", parts);
    }
}