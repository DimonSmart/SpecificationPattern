using System.Linq.Expressions;

namespace DimonSmart.Specification;

public interface IBaseSpecification<T, out TSpecification> where T : class
    where TSpecification : IBaseSpecification<T, TSpecification>
{
    Expression<Func<T, bool>>? WhereExpression { get; }

    public List<(bool direction, Expression<Func<T, object>> expr)> OrderExpressions { get; }

    public int? TakeQ { get; }

    public int? SkipQ { get; }

    TSpecification Where(Expression<Func<T, bool>> expr);

    TSpecification OrderBy(Expression<Func<T, object>> orderByExpression);

    TSpecification OrderByDesc(Expression<Func<T, object>> orderByDescExpression);
}