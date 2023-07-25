using System.Linq.Expressions;
namespace SpecificationPattern;

public interface ISpecification<T>
{
    Expression<Func<T, bool>>? WhereExpression { get; }

    List<(bool direction, Expression<Func<T, object>> expr)> OrderExpressions { get; }

    public int? TakeQ { get; }

    public int? SkipQ { get; }

    ISpecification<T> Or(ISpecification<T> or);

    ISpecification<T> And(ISpecification<T> and);

    ISpecification<T> Where(Expression<Func<T, bool>> expr);
}
