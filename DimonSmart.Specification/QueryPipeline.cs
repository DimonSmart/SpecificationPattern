using System.Collections.Immutable;
using System.Linq.Expressions;

namespace DimonSmart.Specification;

/// <summary>Read-only description of an operation, not an executable query plan.</summary>
public sealed record QueryPipelineStep(
    int Index,
    int StageId,
    string Operation,
    Type InputType,
    Type OutputType,
    IReadOnlyList<LambdaExpression> Expressions,
    bool IsKeysetOrderingBlock);

internal readonly record struct PipelineStage(int Id, bool HasSkip = false, bool HasTake = false, bool HasSeek = false)
{
    internal PipelineStage Next() => new(Id + 1);
}

internal abstract class QueryPipeline<TSource, TCurrent> where TSource : class
{
    internal PipelineStage Stage { get; }

    protected QueryPipeline(PipelineStage stage) => Stage = stage;

    internal abstract IQueryable<TCurrent> Apply(IQueryable<TSource> query, bool criteriaOnly);
    internal abstract void Describe(List<QueryPipelineStep> steps);

    internal IReadOnlyList<QueryPipelineStep> Snapshot()
    {
        var steps = new List<QueryPipelineStep>();
        Describe(steps);
        return steps.AsReadOnly();
    }
}

internal sealed class RootPipeline<TSource> : QueryPipeline<TSource, TSource> where TSource : class
{
    internal RootPipeline() : base(new PipelineStage(0)) { }

    internal override IQueryable<TSource> Apply(IQueryable<TSource> query, bool criteriaOnly) => query;
    internal override void Describe(List<QueryPipelineStep> steps) { }
}

/// <summary>Each node statically connects the input and output element types. Prefixes are shared.</summary>
internal sealed class AppendPipeline<TSource, TBefore, TCurrent> : QueryPipeline<TSource, TCurrent>
    where TSource : class
{
    private readonly QueryPipeline<TSource, TBefore> _previous;
    private readonly Func<IQueryable<TBefore>, IQueryable<TCurrent>> _apply;
    private readonly Func<IQueryable<TBefore>, IQueryable<TCurrent>> _criteria;
    private readonly string _operation;
    private readonly ImmutableArray<LambdaExpression> _expressions;

    internal AppendPipeline(
        QueryPipeline<TSource, TBefore> previous,
        PipelineStage stage,
        string operation,
        Func<IQueryable<TBefore>, IQueryable<TCurrent>> apply,
        Func<IQueryable<TBefore>, IQueryable<TCurrent>> criteria,
        params LambdaExpression[] expressions) : base(stage)
    {
        _previous = previous;
        _operation = operation;
        _apply = apply;
        _criteria = criteria;
        _expressions = expressions.ToImmutableArray();
    }

    internal override IQueryable<TCurrent> Apply(IQueryable<TSource> query, bool criteriaOnly)
    {
        var input = _previous.Apply(query, criteriaOnly);
        return criteriaOnly ? _criteria(input) : _apply(input);
    }

    internal override void Describe(List<QueryPipelineStep> steps)
    {
        _previous.Describe(steps);
        steps.Add(new QueryPipelineStep(steps.Count, Stage.Id, _operation,
            typeof(TBefore), typeof(TCurrent), _expressions, false));
    }
}

/// <summary>One persistent ordering block keeps ThenBy's IOrderedQueryable and seek-before-order semantics.</summary>
internal sealed class OrderedPipeline<TSource, TCurrent> : QueryPipeline<TSource, TCurrent>
    where TSource : class
{
    private readonly QueryPipeline<TSource, TCurrent> _previous;
    internal ImmutableArray<IOrderClause<TCurrent>> Clauses { get; }
    internal SeekBoundary? Boundary { get; }

    internal OrderedPipeline(QueryPipeline<TSource, TCurrent> previous,
        ImmutableArray<IOrderClause<TCurrent>> clauses, SeekBoundary? boundary = null,
        PipelineStage? stage = null) : base(stage ?? previous.Stage)
    {
        _previous = previous;
        Clauses = clauses;
        Boundary = boundary;
    }

    internal OrderedPipeline<TSource, TCurrent> AddKey<TKey>(
        Expression<Func<TCurrent, TKey>> selector, OrderDirectionEnum direction)
    {
        if (Boundary is not null)
            throw new InvalidOperationException("ThenBy cannot follow SeekAfter.");
        return new OrderedPipeline<TSource, TCurrent>(_previous,
            Clauses.Add(new OrderClause<TCurrent, TKey>(selector, direction)), Boundary, Stage);
    }

    internal OrderedPipeline<TSource, TCurrent> Seek<TCursor>(TCursor cursor)
    {
        ArgumentNullException.ThrowIfNull(cursor);
        if (Stage.HasSeek)
            throw new InvalidOperationException("SeekAfter is already configured in this stage.");
        if (Stage.HasSkip)
            throw new InvalidOperationException("SeekAfter cannot be combined with Skip in the same stage.");
        if (Stage.HasTake)
            throw new InvalidOperationException("Take before SeekAfter is not supported in the same stage.");
        return new OrderedPipeline<TSource, TCurrent>(_previous, Clauses,
            SeekBoundary.Create(cursor!, Clauses), Stage with { HasSeek = true });
    }

    internal override IQueryable<TCurrent> Apply(IQueryable<TSource> query, bool criteriaOnly)
    {
        IQueryable<TCurrent> result = _previous.Apply(query, criteriaOnly);
        if (criteriaOnly)
            return result;
        if (Boundary is { } boundary)
            result = result.Where(SeekPredicateBuilder.Build(Clauses, boundary));
        IOrderedQueryable<TCurrent> ordered = Clauses[0].ApplyFirst(result);
        for (var i = 1; i < Clauses.Length; i++)
            ordered = Clauses[i].ApplyNext(ordered);
        return ordered;
    }

    internal override void Describe(List<QueryPipelineStep> steps)
    {
        _previous.Describe(steps);
        for (var i = 0; i < Clauses.Length; i++)
        {
            var clause = Clauses[i];
            var name = i == 0
                ? (clause.Direction == OrderDirectionEnum.Ascending ? "OrderBy" : "OrderByDesc")
                : (clause.Direction == OrderDirectionEnum.Ascending ? "ThenBy" : "ThenByDesc");
            steps.Add(new QueryPipelineStep(steps.Count, Stage.Id, name,
                typeof(TCurrent), typeof(TCurrent),
                ImmutableArray.Create(clause.KeySelector), Boundary is not null));
        }
        if (Boundary is not null)
            steps.Add(new QueryPipelineStep(steps.Count, Stage.Id, "SeekAfter",
                typeof(TCurrent), typeof(TCurrent), ImmutableArray<LambdaExpression>.Empty, true));
    }
}

internal static class PipelineOperations
{
    internal static QueryPipeline<TSource, TCurrent> Where<TSource, TCurrent>(
        QueryPipeline<TSource, TCurrent> pipeline, Expression<Func<TCurrent, bool>> expression)
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(expression);
        return new AppendPipeline<TSource, TCurrent, TCurrent>(
            pipeline, pipeline.Stage, "Where", q => q.Where(expression), q => q.Where(expression), expression);
    }

    internal static QueryPipeline<TSource, TCurrent> OrderBy<TSource, TCurrent, TKey>(
        QueryPipeline<TSource, TCurrent> pipeline, Expression<Func<TCurrent, TKey>> expression,
        OrderDirectionEnum direction) where TSource : class
    {
        ArgumentNullException.ThrowIfNull(expression);
        if (pipeline.Stage.HasSeek)
            throw new InvalidOperationException("OrderBy cannot follow SeekAfter in the same stage.");
        return new OrderedPipeline<TSource, TCurrent>(pipeline,
            ImmutableArray.Create<IOrderClause<TCurrent>>(new OrderClause<TCurrent, TKey>(expression, direction)));
    }

    internal static QueryPipeline<TSource, TCurrent> ThenBy<TSource, TCurrent, TKey>(
        QueryPipeline<TSource, TCurrent> pipeline, Expression<Func<TCurrent, TKey>> expression,
        OrderDirectionEnum direction) where TSource : class
    {
        ArgumentNullException.ThrowIfNull(expression);
        if (pipeline is not OrderedPipeline<TSource, TCurrent> ordered)
            throw new InvalidOperationException("ThenBy requires an existing OrderBy or OrderByDesc immediately preceding it (or ThenBy).");
        return ordered.AddKey(expression, direction);
    }

    internal static QueryPipeline<TSource, TCurrent> SeekAfter<TSource, TCurrent, TCursor>(
        QueryPipeline<TSource, TCurrent> pipeline, TCursor cursor) where TSource : class
    {
        ArgumentNullException.ThrowIfNull(cursor);
        if (pipeline is not OrderedPipeline<TSource, TCurrent> ordered)
            throw new InvalidOperationException("SeekAfter requires an immediately preceding OrderBy or ThenBy.");
        return ordered.Seek(cursor);
    }

    internal static QueryPipeline<TSource, TCurrent> Skip<TSource, TCurrent>(
        QueryPipeline<TSource, TCurrent> pipeline, int count) where TSource : class
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (pipeline.Stage.HasSeek)
            throw new InvalidOperationException("Skip cannot be combined with SeekAfter in the same stage.");
        return new AppendPipeline<TSource, TCurrent, TCurrent>(pipeline,
            pipeline.Stage with { HasSkip = true }, "Skip", q => q.Skip(count), q => q);
    }

    internal static QueryPipeline<TSource, TCurrent> Take<TSource, TCurrent>(
        QueryPipeline<TSource, TCurrent> pipeline, int count) where TSource : class
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return new AppendPipeline<TSource, TCurrent, TCurrent>(pipeline,
            pipeline.Stage with { HasTake = true }, "Take", q => q.Take(count), q => q);
    }

    internal static QueryPipeline<TSource, TResult> Select<TSource, TCurrent, TResult>(
        QueryPipeline<TSource, TCurrent> pipeline, Expression<Func<TCurrent, TResult>> selector)
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(selector);
        return new AppendPipeline<TSource, TCurrent, TResult>(pipeline, pipeline.Stage.Next(),
            "Select", q => q.Select(selector), q => q.Select(selector), selector);
    }

    internal static QueryPipeline<TSource, TCollection> SelectMany<TSource, TCurrent, TCollection>(
        QueryPipeline<TSource, TCurrent> pipeline,
        Expression<Func<TCurrent, IEnumerable<TCollection>>> selector) where TSource : class
    {
        ArgumentNullException.ThrowIfNull(selector);
        return new AppendPipeline<TSource, TCurrent, TCollection>(pipeline, pipeline.Stage.Next(),
            "SelectMany", q => q.SelectMany(selector), q => q.SelectMany(selector), selector);
    }

    internal static QueryPipeline<TSource, TResult> SelectMany<TSource, TCurrent, TCollection, TResult>(
        QueryPipeline<TSource, TCurrent> pipeline,
        Expression<Func<TCurrent, IEnumerable<TCollection>>> collection,
        Expression<Func<TCurrent, TCollection, TResult>> result) where TSource : class
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(result);
        return new AppendPipeline<TSource, TCurrent, TResult>(pipeline, pipeline.Stage.Next(),
            "SelectMany", q => q.SelectMany(collection, result),
            q => q.SelectMany(collection, result), collection, result);
    }
}
