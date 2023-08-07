using System.Linq.Expressions;

namespace DimonSmart.Specification;

public interface IBaseSpecification<T, out TSpecification> where T : class
    where TSpecification : IBaseSpecification<T, TSpecification>
{
    ISpecificationData<T> SpecificationData { get; }

    /// <summary>
    /// Return filtering condition or null if not specified
    /// </summary>
    /// <returns>Where condition</returns>
    Expression<Func<T, bool>>? GetWhereExpression();

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