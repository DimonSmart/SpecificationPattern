using System.Linq.Expressions;

namespace DimonSmart.EFSpecification;

public static class EFIncludeSpecificationExtensions
{
    public static IEFIncludeSpecification<T, TProperty> Include<T, TProperty>(
        this IEFSpecification<T> specification,
        Expression<Func<T, TProperty>> includeExpression) where T : class
    {
        // specification.ResetIncludeLevel();
        specification.AddInclude(GetPropertyName(includeExpression));
        return new EFIncludeSpecification<T, TProperty>(specification);
    }

    public static IEFIncludeSpecification<T, TProperty> ThenInclude<T, TPreviousProperty, TProperty>(
        this IEFIncludeSpecification<T, IEnumerable<TPreviousProperty>> specification,
        Expression<Func<TPreviousProperty, TProperty>> thenIncludeExpression) where T : class
    {
        specification.AddInclude(GetPropertyName(thenIncludeExpression));
        return new EFIncludeSpecification<T, TProperty>(specification);
    }

    public static IEFIncludeSpecification<T, TProperty> ThenInclude<T, TPreviousProperty, TProperty>(
        this IEFIncludeSpecification<T, TPreviousProperty> specification,
        Expression<Func<TPreviousProperty, TProperty>> thenIncludeExpression) where T : class
    {
        specification.AddInclude(GetPropertyName(thenIncludeExpression));
        return new EFIncludeSpecification<T, TProperty>(specification);
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