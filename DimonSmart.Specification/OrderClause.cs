using System.Linq.Expressions;

namespace DimonSmart.Specification;

/// <summary>Typed, immutable executable ordering clause.</summary>
internal interface IOrderClause<TEntity>
{
    OrderDirectionEnum Direction { get; }
    LambdaExpression KeySelector { get; }

    IOrderedQueryable<TEntity> ApplyFirst(IQueryable<TEntity> query);
    IOrderedQueryable<TEntity> ApplyNext(IOrderedQueryable<TEntity> query);
    (Expression Equal, Expression After) BuildSeekComparison(ParameterExpression parameter, object value);
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

    public (Expression Equal, Expression After) BuildSeekComparison(ParameterExpression parameter, object value)
    {
        var key = new ParameterReplacer(Selector.Parameters[0], parameter).Visit(Selector.Body)!;
        var typedValue = (TKey)value;
        // Capturing the value lets EF Core parameterize it instead of embedding a SQL literal.
        Expression<Func<TKey>> cursor = () => typedValue;
        var after = Direction == OrderDirectionEnum.Ascending
            ? Expression.GreaterThan(key, cursor.Body)
            : Expression.LessThan(key, cursor.Body);
        return (Expression.Equal(key, cursor.Body), after);
    }
}
