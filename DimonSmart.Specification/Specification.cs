using System.Collections.Immutable;
using System.Linq.Expressions;

namespace DimonSmart.Specification;

/// <summary>Immutable specification over a query's original entity stage.</summary>
public class Specification<T> : ISpecification<T> where T : class
{
    private readonly QueryPipeline<T, T> _pipeline;
    private readonly Lazy<Func<T, bool>> _compiledCriteria;

    internal Specification(QueryPipeline<T, T> pipeline)
    {
        _pipeline = pipeline;
        _compiledCriteria = new Lazy<Func<T, bool>>(CompileCriteria);
    }

    protected Specification(Specification<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _pipeline = source._pipeline;
        _compiledCriteria = source._compiledCriteria;
    }

    internal QueryPipeline<T, T> Pipeline => _pipeline;
    public IReadOnlyList<QueryPipelineStep> PipelineSteps => _pipeline.Snapshot();

    /// <summary>
    /// Checks only filtering criteria against one entity. Ordering, paging and seek boundaries
    /// are ignored, matching BySpecificationCriteria. The predicate must support LINQ-to-Objects.
    /// The compiled predicate is cached per immutable specification instance.
    /// </summary>
    public bool IsSatisfiedBy(T candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return _compiledCriteria.Value(candidate);
    }

    private Func<T, bool> CompileCriteria()
    {
        Expression<Func<T, bool>>? predicate = null;
        foreach (var operation in _pipeline.Operations())
        {
            if (operation.Kind != "Where")
                continue;
            var current = (Expression<Func<T, bool>>)operation.Predicate!;

            predicate = predicate is null
                ? current
                : ExpressionOperations.CombineExpressions(predicate, current, Expression.AndAlso);
        }

        return predicate is null ? static _ => true : predicate.Compile();
    }

    IQueryable<T> ISpecification<T, T>.ApplyTo(IQueryable<T> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return _pipeline.Apply(query, false);
    }

    IQueryable<T> ISpecification<T, T>.ApplyCriteriaTo(IQueryable<T> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return _pipeline.Apply(query, true);
    }

    private static Specification<T> Append(QueryPipeline<T, T> next) => new(next);

    public Specification<T> Where(Expression<Func<T, bool>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return Append(PipelineOperations.Where(_pipeline, expression));
    }

    public Specification<T> Where(Expression<Func<T, bool>> expression, bool condition)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return condition ? Where(expression) : this;
    }

    public Specification<T> OrderBy<TKey>(Expression<Func<T, TKey>> expression) =>
        AddOrdering(expression, OrderDirectionEnum.Ascending, false);

    public Specification<T> OrderBy<TKey>(Expression<Func<T, TKey>> expression, bool condition)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return condition ? OrderBy(expression) : this;
    }

    public Specification<T> OrderByDesc<TKey>(Expression<Func<T, TKey>> expression) =>
        AddOrdering(expression, OrderDirectionEnum.Descending, false);

    public Specification<T> OrderByDesc<TKey>(Expression<Func<T, TKey>> expression, bool condition)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return condition ? OrderByDesc(expression) : this;
    }

    public Specification<T> ThenBy<TKey>(Expression<Func<T, TKey>> expression) =>
        AddOrdering(expression, OrderDirectionEnum.Ascending, true);

    public Specification<T> ThenBy<TKey>(Expression<Func<T, TKey>> expression, bool condition)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return condition ? ThenBy(expression) : this;
    }

    public Specification<T> ThenByDesc<TKey>(Expression<Func<T, TKey>> expression) =>
        AddOrdering(expression, OrderDirectionEnum.Descending, true);

    public Specification<T> ThenByDesc<TKey>(Expression<Func<T, TKey>> expression, bool condition)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return condition ? ThenByDesc(expression) : this;
    }

    private Specification<T> AddOrdering<TKey>(
        Expression<Func<T, TKey>> expression, OrderDirectionEnum direction, bool subsequent)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var next = subsequent
            ? PipelineOperations.ThenBy(_pipeline, expression, direction)
            : PipelineOperations.OrderBy(_pipeline, expression, direction);
        return Append(next);
    }

    public Specification<T> Skip(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return Append(PipelineOperations.Skip(_pipeline, count));
    }

    public Specification<T> Skip(int count, bool condition)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return condition ? Skip(count) : this;
    }

    public Specification<T> Take(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return Append(PipelineOperations.Take(_pipeline, count));
    }

    public Specification<T> Take(int count, bool condition)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return condition ? Take(count) : this;
    }

    public Specification<T> SeekAfter<TKey>(TKey cursor) =>
        Append(PipelineOperations.SeekAfter(_pipeline,
            clauses => SeekBoundary<T>.Create(clauses, SeekKey<T>.From(cursor))));

    public Specification<T> SeekAfter<T1, T2>((T1, T2) cursor) =>
        Append(PipelineOperations.SeekAfter(_pipeline,
            clauses => SeekBoundary<T>.Create(clauses,
            SeekKey<T>.From(cursor.Item1),
            SeekKey<T>.From(cursor.Item2))));

    public Specification<T> SeekAfter<T1, T2, T3>((T1, T2, T3) cursor) =>
        Append(PipelineOperations.SeekAfter(_pipeline,
            clauses => SeekBoundary<T>.Create(clauses,
            SeekKey<T>.From(cursor.Item1),
            SeekKey<T>.From(cursor.Item2),
            SeekKey<T>.From(cursor.Item3))));

    public Specification<T> SeekAfter<T1, T2, T3, T4>((T1, T2, T3, T4) cursor) =>
        Append(PipelineOperations.SeekAfter(_pipeline,
            clauses => SeekBoundary<T>.Create(clauses,
            SeekKey<T>.From(cursor.Item1),
            SeekKey<T>.From(cursor.Item2),
            SeekKey<T>.From(cursor.Item3),
            SeekKey<T>.From(cursor.Item4))));

    public Specification<T> SeekAfter<T1, T2, T3, T4, T5>((T1, T2, T3, T4, T5) cursor) =>
        Append(PipelineOperations.SeekAfter(_pipeline,
            clauses => SeekBoundary<T>.Create(clauses,
            SeekKey<T>.From(cursor.Item1),
            SeekKey<T>.From(cursor.Item2),
            SeekKey<T>.From(cursor.Item3),
            SeekKey<T>.From(cursor.Item4),
            SeekKey<T>.From(cursor.Item5))));

    public Specification<T> SeekAfter<T1, T2, T3, T4, T5, T6>((T1, T2, T3, T4, T5, T6) cursor) =>
        Append(PipelineOperations.SeekAfter(_pipeline,
            clauses => SeekBoundary<T>.Create(clauses,
            SeekKey<T>.From(cursor.Item1),
            SeekKey<T>.From(cursor.Item2),
            SeekKey<T>.From(cursor.Item3),
            SeekKey<T>.From(cursor.Item4),
            SeekKey<T>.From(cursor.Item5),
            SeekKey<T>.From(cursor.Item6))));

    public Specification<T> SeekAfter<T1, T2, T3, T4, T5, T6, T7>((T1, T2, T3, T4, T5, T6, T7) cursor) =>
        Append(PipelineOperations.SeekAfter(_pipeline,
            clauses => SeekBoundary<T>.Create(clauses,
            SeekKey<T>.From(cursor.Item1),
            SeekKey<T>.From(cursor.Item2),
            SeekKey<T>.From(cursor.Item3),
            SeekKey<T>.From(cursor.Item4),
            SeekKey<T>.From(cursor.Item5),
            SeekKey<T>.From(cursor.Item6),
            SeekKey<T>.From(cursor.Item7))));

    public ProjectedSpecification<T, TResult> Select<TResult>(Expression<Func<T, TResult>> selector) =>
        new(PipelineOperations.Select(_pipeline, selector));

    public ProjectedSpecification<T, TCollection> SelectMany<TCollection>(
        Expression<Func<T, IEnumerable<TCollection>>> collectionSelector) =>
        new(PipelineOperations.SelectMany(_pipeline, collectionSelector));

    public ProjectedSpecification<T, TResult> SelectMany<TCollection, TResult>(
        Expression<Func<T, IEnumerable<TCollection>>> collectionSelector,
        Expression<Func<T, TCollection, TResult>> resultSelector) =>
        new(PipelineOperations.SelectMany(_pipeline, collectionSelector, resultSelector));

    /// <summary>Appends the reusable projected pipeline after this specification's operations.</summary>
    public ProjectedSpecification<T, TResult> WithProjectionOf<TResult>(
        ProjectedSpecification<T, TResult> projection)
    {
        ArgumentNullException.ThrowIfNull(projection);
        return new(new ChainedPipeline<T, TResult>(_pipeline, projection.Pipeline));
    }

    public void EnsureCompatibleWith(Specification<T> other) =>
        _ = AnalyzeCombination(other, "EnsureCompatibleWith");

    internal void EnsureCompatibleWith(Specification<T> other, string operation) =>
        _ = AnalyzeCombination(other, operation);

    private (
        (Expression<Func<T, bool>>? Predicate, ImmutableArray<IOrderClause<T>> Orders,
            SeekBoundary<T>? Boundary, int? Skip, int? Take) Left,
        (Expression<Func<T, bool>>? Predicate, ImmutableArray<IOrderClause<T>> Orders,
            SeekBoundary<T>? Boundary, int? Skip, int? Take) Right) AnalyzeCombination(
        Specification<T> other, string operation)
    {
        ArgumentNullException.ThrowIfNull(other);
        var left = SourceOperations<T>.Analyze(_pipeline, operation);
        var right = SourceOperations<T>.Analyze(other._pipeline, operation);
        if (left.Boundary is not null && right.Boundary is not null)
            throw new InvalidOperationException($"Cannot combine using {operation}: both define SeekAfter.");
        if ((left.Boundary is not null || right.Boundary is not null) &&
            (left.Skip.HasValue || right.Skip.HasValue))
            throw new InvalidOperationException($"Cannot combine using {operation}: Skip conflicts with SeekAfter.");
        if (!left.Orders.IsEmpty && !right.Orders.IsEmpty)
            throw new InvalidOperationException($"Cannot combine using {operation}: conflicting ordering.");
        if (left.Skip.HasValue && right.Skip.HasValue)
            throw new InvalidOperationException($"Cannot combine using {operation}: competing Skip.");
        if (left.Take.HasValue && right.Take.HasValue)
            throw new InvalidOperationException($"Cannot combine using {operation}: competing Take.");
        return (left, right);
    }

    internal Specification<T> Combine(Specification<T> other, bool and)
    {
        var (left, right) = AnalyzeCombination(other, and ? "And" : "Or");
        Expression<Func<T, bool>>? predicate;
        if (and)
            predicate = left.Predicate is null ? right.Predicate : right.Predicate is null
                ? left.Predicate
                : ExpressionOperations.CombineExpressions(left.Predicate, right.Predicate, Expression.AndAlso);
        else
            predicate = left.Predicate is null || right.Predicate is null ? null
                : ExpressionOperations.CombineExpressions(left.Predicate, right.Predicate, Expression.OrElse);

        var combined = Specification.Create<T>();
        if (predicate is not null)
            combined = combined.Where(predicate);

        var orders = !left.Orders.IsEmpty ? left.Orders : right.Orders;
        var boundary = left.Boundary ?? right.Boundary;
        if (!orders.IsEmpty)
        {
            QueryPipeline<T, T> next = new OrderedPipeline<T, T>(
                combined._pipeline, orders, boundary,
                boundary is null ? null : combined._pipeline.Stage with { HasSeek = true });
            combined = new Specification<T>(next);
        }

        var skip = left.Skip ?? right.Skip;
        var take = left.Take ?? right.Take;
        if (skip.HasValue) combined = combined.Skip(skip.Value);
        if (take.HasValue) combined = combined.Take(take.Value);
        return combined;
    }
}

/// <summary>
/// Validates and reads the canonical source-stage view of a query pipeline for And/Or.
/// This is computed on demand; the executable pipeline remains the only stored state.
/// </summary>
internal static class SourceOperations<T> where T : class
{
    internal static (
        Expression<Func<T, bool>>? Predicate,
        ImmutableArray<IOrderClause<T>> Orders,
        SeekBoundary<T>? Boundary,
        int? Skip,
        int? Take) Analyze(QueryPipeline<T, T> pipeline, string operation)
    {
        Expression<Func<T, bool>>? predicate = null;
        var orders = ImmutableArray.CreateBuilder<IOrderClause<T>>();
        SeekBoundary<T>? boundary = null;
        int? skip = null;
        int? take = null;
        // 0: predicates, 1: ordering, 2: seek, 3: skip, 4: take
        var phase = 0;

        foreach (var item in pipeline.Operations())
        {
            switch (item.Kind)
            {
                case "Where":
                    if (phase != 0)
                        throw new InvalidOperationException($"Cannot use {operation}: late Where after query configuration.");
                    var current = (Expression<Func<T, bool>>)item.Predicate!;
                    predicate = predicate is null ? current :
                        ExpressionOperations.CombineExpressions(predicate, current, Expression.AndAlso);
                    break;
                case "OrderBy":
                    if (phase != 0)
                        throw new InvalidOperationException($"Cannot use {operation}: conflicting ordering blocks.");
                    phase = 1;
                    if (item is not OrderingPipelineOperation<T> ordering)
                        throw new InvalidOperationException("Invalid ordering operation in the source pipeline.");
                    orders.Add(ordering.Clause);
                    break;
                case "ThenBy":
                    if (phase != 1)
                        throw new InvalidOperationException($"Cannot use {operation}: disjoint ThenBy.");
                    if (item is not OrderingPipelineOperation<T> nextOrdering)
                        throw new InvalidOperationException("Invalid ordering operation in the source pipeline.");
                    orders.Add(nextOrdering.Clause);
                    break;
                case "SeekAfter":
                    if (phase != 1 || boundary is not null)
                        throw new InvalidOperationException($"Cannot use {operation}: conflicting SeekAfter.");
                    phase = 2;
                    if (item is not SeekPipelineOperation<T> seek)
                        throw new InvalidOperationException("Invalid SeekAfter operation in the source pipeline.");
                    boundary = seek.Boundary;
                    break;
                case "Skip":
                    if (phase > 3 || skip.HasValue || boundary is not null)
                        throw new InvalidOperationException($"Cannot use {operation}: competing Skip or SeekAfter.");
                    phase = 3;
                    skip = item.Count;
                    break;
                case "Take":
                    if (phase > 4 || take.HasValue)
                        throw new InvalidOperationException($"Cannot use {operation}: competing Take.");
                    phase = 4;
                    take = item.Count;
                    break;
            }
        }

        return (predicate, orders.ToImmutable(), boundary, skip, take);
    }
}

public static class Specification
{
    public static Specification<T> Create<T>() where T : class =>
        new(new RootPipeline<T>());
}

public static class SpecificationCompositionExtensions
{
    public static Specification<T> And<T>(this Specification<T> left, Specification<T> right) where T : class =>
        left.Combine(right, true);

    public static Specification<T> Or<T>(this Specification<T> left, Specification<T> right) where T : class =>
        left.Combine(right, false);
}
