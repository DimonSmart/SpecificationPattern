using System.Linq.Expressions;

namespace DimonSmart.Specification.EntityFrameworkCore;

/// <summary>A continuable projected specification retaining immutable root EF Core options.</summary>
public sealed class EFCoreProjectedSpecification<TSource, TCurrent> : IEFCoreSpecification<TSource, TCurrent> where TSource : class
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
    public IEFCoreSpecificationData<TSource> EFCoreSpecificationData => new EFCoreSpecificationData<TSource>(Options);

    IQueryable<TCurrent> ISpecification<TSource, TCurrent>.ApplyTo(IQueryable<TSource> query) =>
        query.BySpecification(this);

    IQueryable<TCurrent> ISpecification<TSource, TCurrent>.ApplyCriteriaTo(IQueryable<TSource> query) =>
        query.BySpecificationCriteria(this);

    private EFCoreProjectedSpecification<TSource, TCurrent> With(
        ProjectedSpecification<TSource, TCurrent> next) => new(next, Options);

    /// <summary>Applies SQL LIKE to the current projection stage without client evaluation.</summary>
    public EFCoreProjectedSpecification<TSource, TCurrent> Search(
        Expression<Func<TCurrent, string?>> selector, string pattern) =>
        Where(EFCoreSearchPredicate.Build(selector, pattern));

    public EFCoreProjectedSpecification<TSource, TCurrent> Search(
        Expression<Func<TCurrent, string?>> selector, string pattern, bool condition)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(pattern);
        return condition ? Search(selector, pattern) : this;
    }

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

    public EFCoreProjectedSpecification<TSource, TCurrent> SeekAfter<TKey>(TKey cursor) =>
        With(Projection.SeekAfter(cursor));

    public EFCoreProjectedSpecification<TSource, TCurrent> SeekAfter<T1, T2>((T1, T2) cursor) =>
        With(Projection.SeekAfter(cursor));

    public EFCoreProjectedSpecification<TSource, TCurrent> SeekAfter<T1, T2, T3>((T1, T2, T3) cursor) =>
        With(Projection.SeekAfter(cursor));

    public EFCoreProjectedSpecification<TSource, TCurrent> SeekAfter<T1, T2, T3, T4>((T1, T2, T3, T4) cursor) =>
        With(Projection.SeekAfter(cursor));

    public EFCoreProjectedSpecification<TSource, TCurrent> SeekAfter<T1, T2, T3, T4, T5>((T1, T2, T3, T4, T5) cursor) =>
        With(Projection.SeekAfter(cursor));

    public EFCoreProjectedSpecification<TSource, TCurrent> SeekAfter<T1, T2, T3, T4, T5, T6>((T1, T2, T3, T4, T5, T6) cursor) =>
        With(Projection.SeekAfter(cursor));

    public EFCoreProjectedSpecification<TSource, TCurrent> SeekAfter<T1, T2, T3, T4, T5, T6, T7>((T1, T2, T3, T4, T5, T6, T7) cursor) =>
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
