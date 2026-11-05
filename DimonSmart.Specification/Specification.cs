using System.Linq.Expressions;

namespace DimonSmart.Specification;

/// <summary>State markers for the type-safe specification API.</summary>
public abstract class SpecificationState
{
    private protected SpecificationState() { }
}

public sealed class Composable : SpecificationState { }

public sealed class Configured : SpecificationState { }

/// <summary>
/// Immutable, type-state specification. Where preserves the state; ordering and
/// pagination change it to Configured.
/// </summary>
public class Specification<T, TState>
    where T : class
    where TState : SpecificationState
{
    private readonly SpecificationData<T> _data;

    internal Specification(SpecificationData<T> data) => _data = data;

    protected Specification(Specification<T, TState> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _data = Copy(source._data);
    }

    internal SpecificationData<T> Data => _data;

    public ISpecificationData<T> SpecificationData => Copy(_data);

    public Expression<Func<T, bool>>? GetWhereExpression() => _data.WhereExpression;

    internal static SpecificationData<T> Copy(SpecificationData<T> source) => new()
    {
        WhereExpression = source.WhereExpression,
        OrderClauses = new List<IOrderClause<T>>(source.OrderClauses),
        Seek = source.Seek,
        TakeQ = source.TakeQ,
        SkipQ = source.SkipQ
    };

    /// <summary>Creates a terminal projection without changing this specification.</summary>
    public ProjectedSpecification<T, TResult> Select<TResult>(Expression<Func<T, TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return new ProjectedSpecification<T, TResult>(_data, selector);
    }

    public Specification<T, TState> Where(Expression<Func<T, bool>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var data = Copy(_data);
        data.WhereExpression = data.WhereExpression is null ? expression :
            ExpressionOperations.CombineExpressions(data.WhereExpression, expression, Expression.AndAlso);
        return new Specification<T, TState>(data);
    }

    /// <summary>Adds a filter only when <paramref name="condition"/> is true.</summary>
    public Specification<T, TState> Where(Expression<Func<T, bool>> expression, bool condition)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return condition ? Where(expression) : this;
    }

    public Specification<T, Configured> OrderBy<TKey>(Expression<Func<T, TKey>> expression) =>
        AppendOrdering(expression, OrderDirectionEnum.Ascending, requireExisting: false);

    /// <summary>Adds an ascending key when enabled. A skipped call still returns Configured.</summary>
    public Specification<T, Configured> OrderBy<TKey>(Expression<Func<T, TKey>> expression, bool condition)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return condition ? OrderBy(expression) : AsConfiguredWithoutChanges();
    }

    public Specification<T, Configured> OrderByDesc<TKey>(Expression<Func<T, TKey>> expression) =>
        AppendOrdering(expression, OrderDirectionEnum.Descending, requireExisting: false);

    /// <summary>Adds a descending key when enabled. A skipped call still returns Configured.</summary>
    public Specification<T, Configured> OrderByDesc<TKey>(Expression<Func<T, TKey>> expression, bool condition)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return condition ? OrderByDesc(expression) : AsConfiguredWithoutChanges();
    }

    /// <summary>Adds a secondary ascending key to an existing ordering.</summary>
    public Specification<T, Configured> ThenBy<TKey>(Expression<Func<T, TKey>> expression) =>
        AppendOrdering(expression, OrderDirectionEnum.Ascending, requireExisting: true);

    /// <summary>Skips the key when disabled; otherwise requires an existing ordering.</summary>
    public Specification<T, Configured> ThenBy<TKey>(Expression<Func<T, TKey>> expression, bool condition)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return condition ? ThenBy(expression) : AsConfiguredWithoutChanges();
    }

    /// <summary>Adds a secondary descending key to an existing ordering.</summary>
    public Specification<T, Configured> ThenByDesc<TKey>(Expression<Func<T, TKey>> expression) =>
        AppendOrdering(expression, OrderDirectionEnum.Descending, requireExisting: true);

    /// <summary>Skips the key when disabled; otherwise requires an existing ordering.</summary>
    public Specification<T, Configured> ThenByDesc<TKey>(Expression<Func<T, TKey>> expression, bool condition)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return condition ? ThenByDesc(expression) : AsConfiguredWithoutChanges();
    }

    // With Composable input, a new wrapper is required by the Configured return type.
    // The immutable data can be shared because no query option was applied.
    private Specification<T, Configured> AsConfiguredWithoutChanges() =>
        typeof(TState) == typeof(Configured)
            ? (Specification<T, Configured>)(object)this
            : new Specification<T, Configured>(_data);

    private Specification<T, Configured> AppendOrdering<TKey>(
        Expression<Func<T, TKey>> expression, OrderDirectionEnum direction, bool requireExisting)
    {
        ArgumentNullException.ThrowIfNull(expression);
        if (_data.Seek is not null)
            throw new InvalidOperationException("Cannot add ordering after SeekAfter.");
        if (requireExisting && _data.OrderClauses.Count == 0)
            throw new InvalidOperationException(
                $"{(direction == OrderDirectionEnum.Ascending ? "ThenBy" : "ThenByDesc")} requires an existing OrderBy or OrderByDesc.");

        var data = Copy(_data);
        data.OrderClauses.Add(new OrderClause<T, TKey>(expression, direction));
        return new Specification<T, Configured>(data);
    }

    public Specification<T, Configured> Take(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var data = Copy(_data);
        data.TakeQ = count;
        return new Specification<T, Configured>(data);
    }

    /// <summary>Applies Take only when enabled; negative counts are always rejected.</summary>
    public Specification<T, Configured> Take(int count, bool condition)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return condition ? Take(count) : AsConfiguredWithoutChanges();
    }

    public Specification<T, Configured> Skip(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (_data.Seek is not null)
            throw new InvalidOperationException("Skip cannot be combined with SeekAfter.");
        var data = Copy(_data);
        data.SkipQ = count;
        return new Specification<T, Configured>(data);
    }

    /// <summary>Applies Skip only when enabled; negative counts are always rejected.</summary>
    public Specification<T, Configured> Skip(int count, bool condition)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return condition ? Skip(count) : AsConfiguredWithoutChanges();
    }

    /// <summary>
    /// Returns rows strictly after the supplied key values in the configured ordering.
    /// For multiple keys, pass a ValueTuple with one element per ordering clause.
    /// </summary>
    public Specification<T, Configured> SeekAfter<TCursor>(TCursor cursor)
    {
        ArgumentNullException.ThrowIfNull(cursor);
        if (_data.Seek is not null)
            throw new InvalidOperationException("SeekAfter is already configured.");
        if (_data.SkipQ.HasValue)
            throw new InvalidOperationException("SeekAfter cannot be combined with Skip.");
        var seek = SeekBoundary.Create(cursor, _data.OrderClauses);
        var data = Copy(_data);
        data.Seek = seek;
        return new Specification<T, Configured>(data);
    }

    internal Specification<T, TResult> Combine<TOther, TResult>(
        Specification<T, TOther> other, bool and)
        where TOther : SpecificationState
        where TResult : SpecificationState
    {
        ArgumentNullException.ThrowIfNull(other);
        var right = other._data;
        var left = _data;

        if (left.OrderClauses.Count != 0 && right.OrderClauses.Count != 0)
            throw new InvalidOperationException("Cannot combine specifications: both define ordering.");
        if (left.SkipQ.HasValue && right.SkipQ.HasValue)
            throw new InvalidOperationException("Cannot combine specifications: both define Skip.");
        if (left.TakeQ.HasValue && right.TakeQ.HasValue)
            throw new InvalidOperationException("Cannot combine specifications: both define Take.");
        if (left.Seek is not null && right.Seek is not null)
            throw new InvalidOperationException("Cannot combine specifications: both define SeekAfter.");
        if ((left.Seek is not null || right.Seek is not null) &&
            (left.SkipQ.HasValue || right.SkipQ.HasValue))
            throw new InvalidOperationException("Cannot combine specifications: Skip conflicts with SeekAfter.");

        var result = Copy(left);
        if (right.WhereExpression is not null)
        {
            result.WhereExpression = result.WhereExpression is null
                ? right.WhereExpression
                : ExpressionOperations.CombineExpressions(result.WhereExpression,
                    right.WhereExpression, and ? Expression.AndAlso : Expression.OrElse);
        }

        result.OrderClauses.AddRange(right.OrderClauses);
        result.Seek ??= right.Seek;
        result.SkipQ ??= right.SkipQ;
        result.TakeQ ??= right.TakeQ;
        return new Specification<T, TResult>(result);
    }
}

/// <summary>Creates immutable specifications.</summary>
public static class Specification
{
    public static Specification<T, Composable> Create<T>() where T : class =>
        new(new SpecificationData<T>());
}

public static class SpecificationCompositionExtensions
{
    public static Specification<T, Composable> And<T>(
        this Specification<T, Composable> left, Specification<T, Composable> right) where T : class =>
        left.Combine<Composable, Composable>(right, true);

    public static Specification<T, Configured> And<T>(
        this Specification<T, Composable> left, Specification<T, Configured> right) where T : class =>
        left.Combine<Configured, Configured>(right, true);

    public static Specification<T, Configured> And<T>(
        this Specification<T, Configured> left, Specification<T, Composable> right) where T : class =>
        left.Combine<Composable, Configured>(right, true);

    public static Specification<T, Configured> And<T>(
        this Specification<T, Configured> left, Specification<T, Configured> right) where T : class =>
        left.Combine<Configured, Configured>(right, true);

    public static Specification<T, Composable> Or<T>(
        this Specification<T, Composable> left, Specification<T, Composable> right) where T : class =>
        left.Combine<Composable, Composable>(right, false);

    public static Specification<T, Configured> Or<T>(
        this Specification<T, Composable> left, Specification<T, Configured> right) where T : class =>
        left.Combine<Configured, Configured>(right, false);

    public static Specification<T, Configured> Or<T>(
        this Specification<T, Configured> left, Specification<T, Composable> right) where T : class =>
        left.Combine<Composable, Configured>(right, false);

    public static Specification<T, Configured> Or<T>(
        this Specification<T, Configured> left, Specification<T, Configured> right) where T : class =>
        left.Combine<Configured, Configured>(right, false);
}
