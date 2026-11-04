using System.Linq.Expressions;

namespace DimonSmart.Specification;

/// <summary>Typed, immutable executable ordering clause.</summary>
internal interface IOrderClause<TEntity>
{
    OrderDirectionEnum Direction { get; }
    LambdaExpression KeySelector { get; }

    IOrderedQueryable<TEntity> ApplyFirst(IQueryable<TEntity> query);
    IOrderedQueryable<TEntity> ApplyNext(IOrderedQueryable<TEntity> query);
}

internal sealed record OrderClause<TEntity, TKey>(
    Expression<Func<TEntity, TKey>> Selector,
    OrderDirectionEnum Direction) : IOrderClause<TEntity>
{
    public LambdaExpression KeySelector => Selector;

    public IOrderedQueryable<TEntity> ApplyFirst(IQueryable<TEntity> query) =>
        Direction == OrderDirectionEnum.Ascending
            ? query.OrderBy(Selector)
            : query.OrderByDescending(Selector);

    public IOrderedQueryable<TEntity> ApplyNext(IOrderedQueryable<TEntity> query) =>
        Direction == OrderDirectionEnum.Ascending
            ? query.ThenBy(Selector)
            : query.ThenByDescending(Selector);
}
