using System.Linq.Expressions;

namespace DimonSmart.Specification;

/// <summary>A typed, immutable keyset continuation predicate, applied before ordering.</summary>
internal sealed record SeekBoundary<TEntity>(Expression<Func<TEntity, bool>> Predicate)
{
    internal static SeekBoundary<TEntity> Create(
        IReadOnlyList<IOrderClause<TEntity>> clauses,
        params SeekKey<TEntity>[] keys)
    {
        if (clauses.Count == 0)
            throw new InvalidOperationException("SeekAfter requires at least one OrderBy clause.");
        if (clauses.Count != keys.Length)
            throw new ArgumentException(
                $"SeekAfter requires {clauses.Count} cursor value(s), but received {keys.Length}.", "cursor");

        var entity = Expression.Parameter(typeof(TEntity), "entity");
        Expression? predicate = null;
        for (var i = keys.Length - 1; i >= 0; i--)
        {
            var (equal, after) = keys[i].Build(clauses[i], entity, i);
            predicate = predicate is null
                ? after
                : Expression.OrElse(after, Expression.AndAlso(equal, predicate));
        }

        return new SeekBoundary<TEntity>(Expression.Lambda<Func<TEntity, bool>>(predicate!, entity));
    }

    internal static bool SupportsKeyType(Type type) =>
        type == typeof(int) || type == typeof(long) ||
        type == typeof(short) || type == typeof(byte) ||
        type == typeof(decimal) || type == typeof(DateTime) ||
        type == typeof(DateOnly);
}

/// <summary>Carries a cursor key through a typed closure without object conversion.</summary>
internal readonly record struct SeekKey<TEntity>(
    Func<IOrderClause<TEntity>, ParameterExpression, int, (Expression Equal, Expression After)> Build)
{
    internal static SeekKey<TEntity> From<TKey>(TKey value) =>
        new((clause, parameter, index) =>
        {
            var keyType = clause.KeySelector.ReturnType;
            if (!SeekBoundary<TEntity>.SupportsKeyType(keyType))
                throw new NotSupportedException(
                    $"SeekAfter does not support sorting key type '{keyType}' at index {index}. " +
                    "Use non-nullable numeric or date keys in v1.");

            if (clause is not IOrderClause<TEntity, TKey> typedClause)
                throw new ArgumentException(
                    $"SeekAfter value at index {index} must have type '{keyType}'.", "cursor");

            return typedClause.BuildSeekComparison(parameter, value);
        });
}
