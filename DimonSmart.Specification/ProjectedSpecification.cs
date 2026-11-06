using System.Linq.Expressions;

namespace DimonSmart.Specification;

/// <summary>Immutable, continuable typed stage of a LINQ specification.</summary>
public sealed class ProjectedSpecification<TSource, TCurrent> where TSource : class
{
    internal QueryPipeline<TSource, TCurrent> Pipeline { get; }
    internal ProjectedSpecification(QueryPipeline<TSource, TCurrent> pipeline) => Pipeline = pipeline;

    public IReadOnlyList<QueryPipelineStep> PipelineSteps => Pipeline.Snapshot();

    public ProjectedSpecification<TSource, TCurrent> Where(Expression<Func<TCurrent, bool>> expression) =>
        new(PipelineOperations.Where(Pipeline, expression));

    public ProjectedSpecification<TSource, TCurrent> Where(Expression<Func<TCurrent, bool>> expression, bool condition)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return condition ? Where(expression) : this;
    }

    public ProjectedSpecification<TSource, TCurrent> OrderBy<TKey>(Expression<Func<TCurrent, TKey>> expression) =>
        new(PipelineOperations.OrderBy(Pipeline, expression, OrderDirectionEnum.Ascending));

    public ProjectedSpecification<TSource, TCurrent> OrderBy<TKey>(Expression<Func<TCurrent, TKey>> expression, bool condition)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return condition ? OrderBy(expression) : this;
    }

    public ProjectedSpecification<TSource, TCurrent> OrderByDesc<TKey>(Expression<Func<TCurrent, TKey>> expression) =>
        new(PipelineOperations.OrderBy(Pipeline, expression, OrderDirectionEnum.Descending));

    public ProjectedSpecification<TSource, TCurrent> OrderByDesc<TKey>(Expression<Func<TCurrent, TKey>> expression, bool condition)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return condition ? OrderByDesc(expression) : this;
    }

    public ProjectedSpecification<TSource, TCurrent> ThenBy<TKey>(Expression<Func<TCurrent, TKey>> expression) =>
        new(PipelineOperations.ThenBy(Pipeline, expression, OrderDirectionEnum.Ascending));

    public ProjectedSpecification<TSource, TCurrent> ThenBy<TKey>(Expression<Func<TCurrent, TKey>> expression, bool condition)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return condition ? ThenBy(expression) : this;
    }

    public ProjectedSpecification<TSource, TCurrent> ThenByDesc<TKey>(Expression<Func<TCurrent, TKey>> expression) =>
        new(PipelineOperations.ThenBy(Pipeline, expression, OrderDirectionEnum.Descending));

    public ProjectedSpecification<TSource, TCurrent> ThenByDesc<TKey>(Expression<Func<TCurrent, TKey>> expression, bool condition)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return condition ? ThenByDesc(expression) : this;
    }

    public ProjectedSpecification<TSource, TCurrent> Skip(int count) =>
        new(PipelineOperations.Skip(Pipeline, count));

    public ProjectedSpecification<TSource, TCurrent> Skip(int count, bool condition)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return condition ? Skip(count) : this;
    }

    public ProjectedSpecification<TSource, TCurrent> Take(int count) =>
        new(PipelineOperations.Take(Pipeline, count));

    public ProjectedSpecification<TSource, TCurrent> Take(int count, bool condition)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return condition ? Take(count) : this;
    }

    public ProjectedSpecification<TSource, TCurrent> SeekAfter<TCursor>(TCursor cursor) =>
        new(PipelineOperations.SeekAfter(Pipeline, cursor));

    public ProjectedSpecification<TSource, TResult> Select<TResult>(
        Expression<Func<TCurrent, TResult>> selector) =>
        new(PipelineOperations.Select(Pipeline, selector));

    public ProjectedSpecification<TSource, TCollection> SelectMany<TCollection>(
        Expression<Func<TCurrent, IEnumerable<TCollection>>> collectionSelector) =>
        new(PipelineOperations.SelectMany(Pipeline, collectionSelector));

    public ProjectedSpecification<TSource, TResult> SelectMany<TCollection, TResult>(
        Expression<Func<TCurrent, IEnumerable<TCollection>>> collectionSelector,
        Expression<Func<TCurrent, TCollection, TResult>> resultSelector) =>
        new(PipelineOperations.SelectMany(Pipeline, collectionSelector, resultSelector));
}
