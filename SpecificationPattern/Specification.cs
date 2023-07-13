using System.Linq.Expressions;
namespace SpecificationPattern;

public class Specification<T>
{
    public static Specification<T> Create() => new();
    public static Specification<T> Where(Expression<Func<T, bool>> expr) => new(expr);

    protected Specification()
    {
    }

    protected Specification(Expression<Func<T, bool>> expr)
    {
        WhereExpression = expr;
    }

    public Expression<Func<T, bool>>? WhereExpression { get; private set; }

    public List<(bool direction, Expression<Func<T, object>> expr)> OrderExpressions { get; } =
       new List<(bool direction, Expression<Func<T, object>> expr)>();

    public int? TakeQ { get; private set; }

    public int? SkipQ { get; private set; }

    public Specification<T> OrderBy(Expression<Func<T, object>> orderByExpression)
    {
        OrderExpressions.Add((true, orderByExpression));
        return this;
    }
    public Specification<T> OrderByDesc(Expression<Func<T, object>> orderByExpression)
    {
        OrderExpressions.Add((false, orderByExpression));
        return this;
    }

    public Specification<T> Take(int take)
    {
        TakeQ = take;
        return this;
    }

    public Specification<T> Skip(int skip)
    {
        SkipQ = skip;
        return this;
    }

    public virtual Specification<T> Or(Specification<T> or)
    {
        if (or?.WhereExpression == null)
        {
            return this;
        }

        if (WhereExpression == null)
        {
            WhereExpression = or.WhereExpression;
            return this;
        }

        WhereExpression = ExpressionOperations.CombineExpressions(WhereExpression, or.WhereExpression, Expression.OrElse);
        return this;
    }

    public virtual Specification<T> And(Specification<T> and)
    {
        if (and?.WhereExpression == null)
        {
            return this;
        }

        if (WhereExpression == null)
        {
            WhereExpression = and.WhereExpression;
            return this;
        }

        WhereExpression = ExpressionOperations.CombineExpressions(WhereExpression, and.WhereExpression, Expression.AndAlso);
        return this;
    }

    public Specification<T> Or(Expression<Func<T, bool>>? or)
    {
        if (or == null)
        {
            return this;
        }

        if (WhereExpression == null)
        {
            WhereExpression = or;
            return this;
        }

        WhereExpression = ExpressionOperations.CombineExpressions(WhereExpression, or, Expression.OrElse);

        return this;
    }

    public Specification<T> And(Expression<Func<T, bool>>? and)
    {
        if (and == null)
        {
            return this;
        }

        if (WhereExpression == null)
        {
            WhereExpression = and;
            return this;
        }

        WhereExpression = ExpressionOperations.CombineExpressions(WhereExpression, and, Expression.AndAlso);

        return this;
    }
}
