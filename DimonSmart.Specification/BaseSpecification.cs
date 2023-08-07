using System.Linq.Expressions;
using static DimonSmart.Specification.OrderDirectionEnum;

namespace DimonSmart.Specification;

public abstract class BaseSpecification<T, TSpecification> : IBaseSpecification<T, TSpecification>
    where TSpecification : IBaseSpecification<T, TSpecification> where T : class
{
    private readonly SpecificationData<T> _specificationData = new();
    public ISpecificationData<T> SpecificationData => _specificationData;

    public TSpecification Where(Expression<Func<T, bool>> expr)
    {
        if (_specificationData.WhereExpression == null)
        {
            _specificationData.WhereExpression = expr;
            return AsTSpecification();
        }

        And(expr);
        return AsTSpecification();
    }

    public Expression<Func<T, bool>>? GetWhereExpression()
    {
        return SpecificationData.WhereExpression;
    }

    public TSpecification Take(int take)
    {
        _specificationData.TakeQ = take;
        return AsTSpecification();
    }

    public TSpecification Skip(int skip)
    {
        _specificationData.SkipQ = skip;
        return AsTSpecification();
    }

    public virtual TSpecification OrderBy(Expression<Func<T, object>> orderByExpression)
    {
        SpecificationData.OrderExpressions.Add((Ascending, orderByExpression));
        return AsTSpecification();
    }

    public virtual TSpecification OrderByDesc(Expression<Func<T, object>> orderByExpression)
    {
        SpecificationData.OrderExpressions.Add((Descending, orderByExpression));
        return AsTSpecification();
    }

    public TSpecification And(Expression<Func<T, bool>>? and)
    {
        if (and == null)
        {
            return AsTSpecification();
        }

        if (_specificationData.WhereExpression == null)
        {
            _specificationData.WhereExpression = and;
            return AsTSpecification();
        }

        _specificationData.WhereExpression = ExpressionOperations
            .CombineExpressions(_specificationData.WhereExpression, and, Expression.AndAlso);

        return AsTSpecification();
    }

    public TSpecification Or(Expression<Func<T, bool>>? or)
    {
        if (or == null)
        {
            return AsTSpecification();
        }

        if (_specificationData.WhereExpression == null)
        {
            _specificationData.WhereExpression = or;
            return AsTSpecification();
        }

        _specificationData.WhereExpression = ExpressionOperations
            .CombineExpressions(_specificationData.WhereExpression, or, Expression.Or);

        return AsTSpecification();
    }

    protected abstract TSpecification AsTSpecification();
}