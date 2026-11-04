using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace DimonSmart.Specification;

/// <summary>Internal, immutable continuation values; applied to the final composed query.</summary>
internal sealed record SeekBoundary(ImmutableArray<object> Values)
{
    internal static SeekBoundary Create<TEntity>(object cursor, IReadOnlyList<IOrderClause<TEntity>> clauses)
    {
        if (clauses.Count == 0)
            throw new InvalidOperationException("SeekAfter requires at least one OrderBy clause.");

        object?[] values;
        if (cursor is ITuple tuple)
        {
            values = new object?[tuple.Length];
            for (var i = 0; i < tuple.Length; i++)
                values[i] = tuple[i];
        }
        else
        {
            values = new object?[] { cursor };
        }

        if (values.Length != clauses.Count)
            throw new ArgumentException(
                $"SeekAfter requires {clauses.Count} cursor value(s), but received {values.Length}.", nameof(cursor));

        for (var i = 0; i < clauses.Count; i++)
        {
            var keyType = clauses[i].KeySelector.ReturnType;
            if (!SupportsKeyType(keyType))
                throw new NotSupportedException(
                    $"SeekAfter does not support sorting key type '{keyType}' at index {i}. " +
                    "Use non-nullable numeric or date keys in v1.");
            if (values[i] is null || values[i]!.GetType() != keyType)
                throw new ArgumentException(
                    $"SeekAfter value at index {i} must have type '{keyType}'.", nameof(cursor));
        }

        return new SeekBoundary(values.Select(value => value!).ToImmutableArray());
    }

    private static bool SupportsKeyType(Type type) =>
        type == typeof(int) || type == typeof(long) ||
        type == typeof(short) || type == typeof(byte) ||
        type == typeof(decimal) || type == typeof(DateTime) ||
        type == typeof(DateOnly);
}

/// <summary>Builds a lexicographic continuation expression from the existing typed ordering clauses.</summary>
internal static class SeekPredicateBuilder
{
    internal static Expression<Func<TEntity, bool>> Build<TEntity>(
        IReadOnlyList<IOrderClause<TEntity>> clauses, SeekBoundary boundary)
    {
        var entity = Expression.Parameter(typeof(TEntity), "entity");
        Expression? predicate = null;
        for (var i = clauses.Count - 1; i >= 0; i--)
        {
            var (equal, after) = clauses[i].BuildSeekComparison(entity, boundary.Values[i]);
            predicate = predicate is null
                ? after
                : Expression.OrElse(after, Expression.AndAlso(equal, predicate));
        }

        return Expression.Lambda<Func<TEntity, bool>>(predicate!, entity);
    }
}
