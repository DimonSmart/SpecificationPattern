using System.Linq.Expressions;

namespace DimonSmart.Specification;

/// <summary>
/// Interface for building specifications to filter and order data queries.
/// </summary>
/// <typeparam name="T">The type of entity being queried.</typeparam>
/// <typeparam name="TSpecification">The type of specification being built.</typeparam>
public interface IBaseSpecification<T, out TSpecification> where T : class
    where TSpecification : IBaseSpecification<T, TSpecification>
{
    /// <summary>
    /// Gets the specification data that contains the filter and order information.
    /// </summary>
    ISpecificationData<T> SpecificationData { get; }

    /// <summary>
    /// Returns the filtering condition or null if not specified.
    /// </summary>
    /// <returns>Where condition as an expression.</returns>
    Expression<Func<T, bool>>? GetWhereExpression();

    /// <summary>
    /// Specifies the number of elements to be taken from the query.
    /// </summary>
    /// <param name="take">The number of elements to take.</param>
    /// <returns>The current specification with the take condition applied.</returns>
    TSpecification Take(int take);

    /// <summary>
    /// Specifies the number of elements to be skipped from the query.
    /// </summary>
    /// <param name="skip">The number of elements to skip.</param>
    /// <returns>The current specification with the skip condition applied.</returns>
    TSpecification Skip(int skip);

    /// <summary>
    /// Specifies a filtering condition for the data query.
    /// </summary>
    /// <param name="expr">Logical expression for filtering items.</param>
    /// <returns>The current specification with the new filtering condition applied.</returns>
    TSpecification Where(Expression<Func<T, bool>> expr);

    /// <summary>
    /// Specifies an ascending order for the data query.
    /// Any additional OrderBy specification adds a secondary factor and does not override the previous one.
    /// </summary>
    /// <param name="orderByExpression">The expression to order by in ascending order.</param>
    /// <returns>The current specification with the new ascending order condition applied.</returns>
    TSpecification OrderBy(Expression<Func<T, object>> orderByExpression);

    /// <summary>
    /// Specifies a descending order for the data query.
    /// Any additional OrderByDesc specification adds a secondary factor and does not override the previous one.
    /// </summary>
    /// <param name="orderByDescExpression">The expression to order by in descending order.</param>
    /// <returns>The current specification with the new descending order condition applied.</returns>
    TSpecification OrderByDesc(Expression<Func<T, object>> orderByDescExpression);
}
