using System.Linq.Expressions;

namespace DimonSmart.Specification;

/// <summary>
/// Interface for holding data related to filtering and ordering of query results.
/// </summary>
/// <typeparam name="T">The type of entity being queried.</typeparam>
public interface ISpecificationData<T>
{
    /// <summary>
    /// Gets the expression representing the WHERE clause for data filtering.
    /// </summary>
    /// <remarks>
    /// The expression should evaluate to a Boolean value to filter the query results.
    /// If not specified, no filtering condition will be applied.
    /// </remarks>
    Expression<Func<T, bool>>? WhereExpression { get; }

    /// <summary>
    /// Gets the list of order expressions and their corresponding directions.
    /// </summary>
    /// <remarks>
    /// Each item in the list consists of an expression to order by and the order direction (ascending or descending).
    /// The order expressions will be applied in the specified order of appearance.
    /// </remarks>
    List<(OrderDirectionEnum direction, Expression<Func<T, object>> expr)> OrderExpressions { get; }

    /// <summary>
    /// Gets the number of elements to take from the query result.
    /// </summary>
    /// <remarks>
    /// If specified, the query will return only the specified number of elements.
    /// If not specified, all matching elements will be returned.
    /// </remarks>
    int? TakeQ { get; }

    /// <summary>
    /// Gets the number of elements to skip from the query result.
    /// </summary>
    /// <remarks>
    /// If specified, the query will skip the specified number of elements and start from the next one.
    /// If not specified, no elements will be skipped.
    /// </remarks>
    int? SkipQ { get; }
}