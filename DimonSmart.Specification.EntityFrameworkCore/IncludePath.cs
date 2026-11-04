using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace DimonSmart.Specification.EntityFrameworkCore;

/// <summary>Extracts navigation identity only. Executable expressions are never reconstructed from paths.</summary>
internal static class IncludePath
{
    private static readonly HashSet<string> SupportedOperators =
    [
        nameof(Enumerable.Where),
        nameof(Enumerable.OrderBy),
        nameof(Enumerable.OrderByDescending),
        nameof(Enumerable.ThenBy),
        nameof(Enumerable.ThenByDescending),
        nameof(Enumerable.Skip),
        nameof(Enumerable.Take)
    ];

    internal static IReadOnlyList<IncludeSegment> Parse(LambdaExpression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        Expression current = StripConvert(expression.Body);
        var hasFilter = false;

        while (current is MethodCallExpression call)
        {
            if (call.Method.DeclaringType != typeof(Enumerable) ||
                !SupportedOperators.Contains(call.Method.Name) || call.Arguments.Count < 2)
                throw new NotSupportedException(
                    "Unsupported Include expression operator: " + call.Method.Name +
                    ". Only Where, OrderBy, OrderByDescending, ThenBy, ThenByDescending, Skip and Take are supported.");

            hasFilter = true;
            current = StripConvert(call.Arguments[0]);
        }

        var members = new List<MemberInfo>();
        while (current is MemberExpression member)
        {
            members.Add(member.Member);
            current = member.Expression is null ? throw new NotSupportedException(
                "Include must be rooted at its lambda parameter.") : StripConvert(member.Expression);
        }

        if (current != expression.Parameters[0] || members.Count == 0)
            throw new NotSupportedException(
                "Include must be a navigation member-access chain rooted at the lambda parameter.");

        members.Reverse();
        var result = members.Select(m => new IncludeSegment(m, null)).ToArray();
        if (hasFilter)
        {
            var last = members[^1];
            var propertyType = last switch
            {
                PropertyInfo p => p.PropertyType,
                FieldInfo f => f.FieldType,
                _ => throw new NotSupportedException("Include supports property or field navigation access only.")
            };
            if (propertyType == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(propertyType))
                throw new NotSupportedException("Filtered Include is supported only on collection navigations.");

            result[^1] = new IncludeSegment(last, Guid.NewGuid());
        }

        return result;
    }

    private static Expression StripConvert(Expression expression)
    {
        while (expression is UnaryExpression unary &&
               (unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked))
        {
            if (!unary.Type.IsAssignableFrom(unary.Operand.Type))
                throw new NotSupportedException("Derived-type casts are not supported in Include paths.");
            expression = unary.Operand;
        }
        return expression;
    }
}

internal sealed record IncludeSegment(MemberInfo Member, Guid? FilterId)
{
    internal string Name => Member.Name;
}
