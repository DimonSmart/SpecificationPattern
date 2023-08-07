using System.Linq.Expressions;

namespace DimonSmart.Specification;

public interface IBaseSpecification<T, out TSpecification> where T : class
    where TSpecification : IBaseSpecification<T, TSpecification>
{
    Expression<Func<T, bool>>? WhereExpression { get; }

    public List<(bool direction, Expression<Func<T, object>> expr)> OrderExpressions { get; }

    public int? TakeQ { get; }

    public int? SkipQ { get; }

    /// <summary>
    /// Specify how many elements will be taken from query
    /// </summary>
    /// <param name="take"></param>
    /// <returns></returns>
    TSpecification Take(int take);

    /// <summary>
    /// Specify how many elements should be skipped from query
    /// </summary>
    /// <param name="skip"></param>
    /// <returns></returns>
    TSpecification Skip(int skip);

    /// <summary>
    /// Filtering condition specification
    /// </summary>
    /// <param name="expr">Logical expression for items filtering</param>
    /// <returns></returns>
    TSpecification Where(Expression<Func<T, bool>> expr);

    /// <summary>
    /// Ascending order specification.
    /// Any additional Order specification add second factor specification
    /// and not override the previous one
    /// </summary>
    /// <param name="orderByExpression"></param>
    TSpecification OrderBy(Expression<Func<T, object>> orderByExpression);


    /// <summary>
    /// Descending order specification.
    /// Any additional Order specification add second factor specification
    /// and not override the previous one
    /// </summary>
    /// <param name="orderByDescExpression"></param>
    TSpecification OrderByDesc(Expression<Func<T, object>> orderByDescExpression);
}