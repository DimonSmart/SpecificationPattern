using System.Collections.Immutable;
using System.Linq.Expressions;

namespace DimonSmart.Specification;

public class SpecificationData<T> : ISpecificationData<T>
{
    public Expression<Func<T, bool>>? WhereExpression { get; set; }

    // Executable query configuration is never exposed through the diagnostic API.
    internal List<IOrderClause<T>> OrderClauses { get; set; } = new();

    public IReadOnlyList<(OrderDirectionEnum direction, LambdaExpression expr)> OrderExpressions =>
        OrderClauses.Select(clause => (direction: clause.Direction, expr: clause.KeySelector))
            .ToImmutableArray();

    public int? TakeQ { get; set; }
    public int? SkipQ { get; set; }
}
