using System.Linq.Expressions;

namespace DimonSmart.Specification.EntityFrameworkCore;

internal static class IncludePath
{
    internal static string GetPropertyName<T, TProperty>(Expression<Func<T, TProperty>> expression)
    {
        var body = expression.Body as MemberExpression
            ?? throw new InvalidOperationException("Include must be a property access.");
        var parts = new Stack<string>();
        while (body is not null)
        {
            parts.Push(body.Member.Name);
            body = body.Expression as MemberExpression;
        }
        return string.Join(".", parts);
    }
}
