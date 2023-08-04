using System.Linq.Expressions;

namespace DimonSmart.Specification;

public class Specification<T> : BaseSpecification<T, Specification<T>>, ISpecification<T> where T : class
{
    protected Specification()
    {
    }

    protected Specification(Expression<Func<T, bool>> expr)
    {
        WhereExpression = expr;
    }

    ISpecification<T> IBaseSpecification<T, ISpecification<T>>.Where(Expression<Func<T, bool>> expr)
    {
        return Where(expr);
    }

    ISpecification<T> IBaseSpecification<T, ISpecification<T>>.OrderBy(Expression<Func<T, object>> orderByExpression)
    {
        return OrderBy(orderByExpression);
    }

    ISpecification<T> IBaseSpecification<T, ISpecification<T>>.OrderByDesc(
        Expression<Func<T, object>> orderByDescExpression)
    {
        return OrderByDesc(orderByDescExpression);
    }

    public static Specification<T> Create()
    {
        return new Specification<T>();
    }

    public ISpecification<T> Take(int take)
    {
        TakeQ = take;
        return this;
    }

    public ISpecification<T> Skip(int skip)
    {
        SkipQ = skip;
        return this;
    }

    protected override Specification<T> AsTSpecification()
    {
        return this;
    }
}