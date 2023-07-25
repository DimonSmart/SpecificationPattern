using System.Linq.Expressions;
namespace SpecificationPattern;


public class Specification<T> : ISpecification<T>
{
    public static Specification<T> Create() => new();

    protected Specification()
    {
    }

    protected Specification(Expression<Func<T, bool>> expr)
    {
        WhereExpression = expr;
    }

    public virtual ISpecification<T> Where(Expression<Func<T, bool>> expr) => new Specification<T>(expr);

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

    public virtual ISpecification<T> Or(ISpecification<T> or)
    {
        return Or(or.WhereExpression);
    }

    public virtual ISpecification<T> Or(Expression<Func<T, bool>>? or)
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


    public virtual ISpecification<T> And(ISpecification<T> and)
    {
        return And(and.WhereExpression);
    }

    public virtual ISpecification<T> And(Expression<Func<T, bool>>? and)
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
