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

    /// <summary>
    /// Resolves a string navigation path to the same member identities as typed Include expressions.
    /// EF Core validates whether these CLR members are mapped navigations during execution.
    /// </summary>
    internal static IReadOnlyList<IncludeSegment> Parse<TEntity>(string path) where TEntity : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var parts = path.Split('.');
        if (parts.Any(part => string.IsNullOrWhiteSpace(part) || part != part.Trim()))
            throw new ArgumentException(
                "Include path must contain non-empty navigation names separated by dots.", nameof(path));

        var result = new List<IncludeSegment>(parts.Length);
        var current = typeof(TEntity);
        foreach (var part in parts)
        {
            var members = current.GetMember(part, MemberTypes.Property | MemberTypes.Field,
                BindingFlags.Public | BindingFlags.Instance);
            var member = (MemberInfo?)members.OfType<PropertyInfo>()
                .FirstOrDefault(property => property.GetIndexParameters().Length == 0)
                ?? members.OfType<FieldInfo>().FirstOrDefault();
            if (member is null)
                throw new ArgumentException(
                    $"Include path '{path}' contains unknown CLR member '{part}' on '{current.Name}'. " +
                    "Only public CLR navigation properties and fields are supported.", nameof(path));
            result.Add(new IncludeSegment(member, null));
            var memberType = member switch
            {
                PropertyInfo property => property.PropertyType,
                FieldInfo field => field.FieldType,
                _ => throw new InvalidOperationException("Unexpected Include member type.")
            };
            var collection = memberType == typeof(string) ? null
                : memberType.GetInterfaces().Prepend(memberType)
                    .FirstOrDefault(type => type.IsGenericType &&
                        type.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            current = collection?.GetGenericArguments()[0] ?? memberType;
        }
        return result;
    }

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

            // Compare filtered operations independently of how the navigation was reached.
            var navigation = Expression.Parameter(current.Type, "navigation");
            var normalized = new NavigationReplacer(current, navigation).Visit(expression.Body)!;
            result[^1] = new IncludeSegment(last, Expression.Lambda(normalized, navigation));
        }

        return result;
    }

    private sealed class NavigationReplacer(Expression source, ParameterExpression replacement)
        : ExpressionVisitor
    {
        public override Expression? Visit(Expression? node) =>
            ReferenceEquals(node, source) ? replacement : base.Visit(node);
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

internal sealed record IncludeSegment(MemberInfo Member, LambdaExpression? FilterExpression)
{
    internal string Name => Member.Name;
}
