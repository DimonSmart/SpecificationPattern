using System.Linq.Expressions;
namespace DimonSmart.Specification;

public interface IBaseSpecification<T, out TSpecification> where T: class where TSpecification : IBaseSpecification<T, TSpecification>
{
    Expression<Func<T, bool>>? WhereExpression { get; }

    TSpecification Where(Expression<Func<T, bool>> expr);

    TSpecification OrderBy(Expression<Func<T, object>> orderByExpression);

    TSpecification OrderByDesc(Expression<Func<T, object>> orderByDescExpression);

    public List<(bool direction, Expression<Func<T, object>> expr)> OrderExpressions { get; }

    public int? TakeQ { get; }

    public int? SkipQ { get; }

    //TSpecification Or(Expression<Func<T, bool>> expr);

    //TSpecification And(Expression<Func<T, bool>> expr);
}


public abstract class BaseSpecification<T, TSpecification> : IBaseSpecification<T, TSpecification> where TSpecification : IBaseSpecification<T, TSpecification> where T :class
{
    public Expression<Func<T, bool>>? WhereExpression { get;  protected set; }

    public List<(bool direction, Expression<Func<T, object>> expr)> OrderExpressions { get; } =  new List<(bool direction, Expression<Func<T, object>> expr)>();

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
    
    protected abstract TSpecification AsTSpecification();
}

public interface ISpecification<T> : IBaseSpecification<T, ISpecification<T>> where T : class
{
   // List<(bool direction, Expression<Func<T, object>> expr)> OrderExpressions { get; }

    //public int? TakeQ { get; }

    //public int? SkipQ { get; }

    //abstract ISpecification<T> Or(IWhereSpecification<T> or);

    //ISpecification<T> And(IWhereSpecification<T> and);

    //abstract ISpecification<T> Where(Expression<Func<T, bool>> expr);

    //ISpecification<T> OrderBy(Expression<Func<T, object>> orderByExpression);

    //ISpecification<T> OrderByDesc(Expression<Func<T, object>> orderByExpression);
}
