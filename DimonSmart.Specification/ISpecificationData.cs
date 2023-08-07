using System.Linq.Expressions;

namespace DimonSmart.Specification;

/// <summary>
/// Order direction
/// </summary>
public enum OrderDirectionEnum
{
    Ascending,
    Descending
}


public interface ISpecificationData<T>
{
    /// <summary>
    /// Where clause for data filtering
    /// </summary>
    Expression<Func<T, bool>>? WhereExpression { get; }

    /// <summary>
    /// OrderBy property and direction list
    /// </summary>
    public List<(OrderDirectionEnum direction, Expression<Func<T, object>> expr)> OrderExpressions { get; }

    /// <summary>
    /// Specify how many elements should be skipped
    /// </summary>
    public int? TakeQ { get; }

    /// <summary>
    /// Specify how many elements to take
    /// </summary>
    public int? SkipQ { get; }
}