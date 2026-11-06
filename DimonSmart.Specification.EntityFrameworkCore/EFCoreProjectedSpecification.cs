using System.Linq.Expressions;

namespace DimonSmart.Specification.EntityFrameworkCore;

/// <summary>A continuable projected specification retaining immutable root EF Core options.</summary>
public sealed class EFCoreProjectedSpecification<TSource, TCurrent> where TSource : class
{
    internal ProjectedSpecification<TSource, TCurrent> Projection { get; }
    internal QueryOptions<TSource> Options { get; }

    internal EFCoreProjectedSpecification(
        ProjectedSpecification<TSource, TCurrent> projection, QueryOptions<TSource> options)
    {
        Projection = projection;
        Options = options;
    }

    public IReadOnlyList<QueryPipelineStep> PipelineSteps => Projection.PipelineSteps;

    private EFCoreProjectedSpecification<TSource, TCurrent> With(
        ProjectedSpecification<TSource, TCurrent> next) => new(next, Options);

    public EFCoreProjectedSpecification<TSource, TCurrent> Where(Expression<Func<TCurrent, bool>> expression) =>
        With(Projection.Where(expression));

    public EFCoreProjectedSpecification<TSource, TCurrent> Where(Expression<Func<TCurrent, bool>> expression, bool condition)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return condition ? Where(expression) : this;
    }

    public EFCoreProjectedSpecification<TSource, TCurrent> OrderBy<TKey>(Expression<Func<TCurrent, TKey>> expression) =>
        With(Projection.OrderBy(expression));

    public EFCoreProjectedSpecification<TSource, TCurrent> OrderBy<TKey>(
        Expression<Func<TCurrent, TKey>> expression, bool condition)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return condition ? OrderBy(expression) : this;
    }

    public EFCoreProjectedSpecification<TSource, TCurrent> OrderByDesc<TKey>(Expression<Func<TCurrent, TKey>> expression) =>
        With(Projection.OrderByDesc(expression));

    public EFCoreProjectedSpecification<TSource, TCurrent> OrderByDesc<TKey>(
        Expression<Func<TCurrent, TKey>> expression, bool condition)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return condition ? OrderByDesc(expression) : this;
    }

    public EFCoreProjectedSpecification<TSource, TCurrent> ThenBy<TKey>(Expression<Func<TCurrent, TKey>> expression) =>
        With(Projection.ThenBy(expression));

    public EFCoreProjectedSpecification<TSource, TCurrent> ThenBy<TKey>(
        Expression<Func<TCurrent, TKey>> expression, bool condition)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return condition ? ThenBy(expression) : this;
    }

    public EFCoreProjectedSpecification<TSource, TCurrent> ThenByDesc<TKey>(Expression<Func<TCurrent, TKey>> expression) =>
        With(Projection.ThenByDesc(expression));

    public EFCoreProjectedSpecification<TSource, TCurrent> ThenByDesc<TKey>(
        Expression<Func<TCurrent, TKey>> expression, bool condition)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return condition ? ThenByDesc(expression) : this;
    }

    public EFCoreProjectedSpecification<TSource, TCurrent> Skip(int count) =>
        With(Projection.Skip(count));

    public EFCoreProjectedSpecification<TSource, TCurrent> Skip(int count, bool condition)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return condition ? Skip(count) : this;
    }

    public EFCoreProjectedSpecification<TSource, TCurrent> Take(int count) =>
        With(Projection.Take(count));

    public EFCoreProjectedSpecification<TSource, TCurrent> Take(int count, bool condition)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return condition ? Take(count) : this;
    }

    public EFCoreProjectedSpecification<TSource, TCurrent> SeekAfter<TCursor>(TCursor cursor) =>
        With(Projection.SeekAfter(cursor));

    public EFCoreProjectedSpecification<TSource, TResult> Select<TResult>(
        Expression<Func<TCurrent, TResult>> selector) =>
        new(Projection.Select(selector), Options);

    public EFCoreProjectedSpecification<TSource, TCollection> SelectMany<TCollection>(
        Expression<Func<TCurrent, IEnumerable<TCollection>>> collectionSelector) =>
        new(Projection.SelectMany(collectionSelector), Options);

    public EFCoreProjectedSpecification<TSource, TResult> SelectMany<TCollection, TResult>(
        Expression<Func<TCurrent, IEnumerable<TCollection>>> collectionSelector,
        Expression<Func<TCurrent, TCollection, TResult>> resultSelector) =>
        new(Projection.SelectMany(collectionSelector, resultSelector), Options);
}
