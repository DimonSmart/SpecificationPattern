using System.Linq.Expressions;

namespace DimonSmart.Specification;

public abstract class BaseSpecification<T, TSpecification> : IBaseSpecification<T, TSpecification>
    where TSpecification : IBaseSpecification<T, TSpecification> where T : class
{
   
    public List<(bool direction, Expression<Func<T, object>> expr)> OrderExpressions { get; } = new();

    public TSpecification Where(Expression<Func<T, bool>> expr)
    {
        if (WhereExpression == null)
        {
            WhereExpression = expr;
            return AsTSpecification();
        }

        And(expr);
        return AsTSpecification();
    }
    
    public TSpecification Take(int take)
    {
        TakeQ = take;
        return AsTSpecification();
    }

    public TSpecification Skip(int skip)
    {
        SkipQ = skip;
        return AsTSpecification();
    }

    public Expression<Func<T, bool>>? WhereExpression { get; protected set; }

    public int? TakeQ { get; protected set; }

    public int? SkipQ { get; protected set; }

    public virtual TSpecification OrderBy(Expression<Func<T, object>> orderByExpression)
    {
        OrderExpressions.Add((true, orderByExpression));
        return AsTSpecification();
    }

    public virtual TSpecification OrderByDesc(Expression<Func<T, object>> orderByExpression)
    {
        OrderExpressions.Add((false, orderByExpression));
        return AsTSpecification();
    }

    public TSpecification And(Expression<Func<T, bool>>? and)
    {
        if (and == null)
        {
            return AsTSpecification();
        }

        if (WhereExpression == null)
        {
            WhereExpression = and;
            return AsTSpecification();
        }

        WhereExpression = ExpressionOperations.CombineExpressions(WhereExpression, and, Expression.AndAlso);

        return AsTSpecification();
    }

    public TSpecification Or(Expression<Func<T, bool>>? or)
    {
        if (or == null)
        {
            return AsTSpecification();
        }

        if (WhereExpression == null)
        {
            WhereExpression = or;
            return AsTSpecification();
        }

        WhereExpression = ExpressionOperations.CombineExpressions(WhereExpression, or, Expression.Or);

        return AsTSpecification();
    }

    protected abstract TSpecification AsTSpecification();
}