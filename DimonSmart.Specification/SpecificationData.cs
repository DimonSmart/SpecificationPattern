using System.Linq.Expressions;

namespace DimonSmart.Specification;

public class SpecificationData<T> : ISpecificationData<T>
{
    public Expression<Func<T, bool>>? WhereExpression { get; set; }

    public List<(OrderDirectionEnum direction, Expression<Func<T, object>> expr)> OrderExpressions { get; set; } =
        new();

    public int? TakeQ { get; set; }
    public int? SkipQ { get; set; }
}