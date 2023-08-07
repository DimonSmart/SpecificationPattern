using System.Linq.Expressions;

namespace DimonSmart.Specification;

public class Specification<T> : BaseSpecification<T, Specification<T>>, ISpecification<T> where T : class
{
    private Specification()
    {
    }

    ISpecification<T> IBaseSpecification<T, ISpecification<T>>.Take(int take)
    {
        return Take(take);
    }

    ISpecification<T> IBaseSpecification<T, ISpecification<T>>.Skip(int skip)
    {
        return Skip(skip);
    }

    ISpecification<T> IBaseSpecification<T, ISpecification<T>>.Where(Expression<Func<T, bool>> expr)
    {
        return Where(expr);
    }

    ISpecification<T> IBaseSpecification<T, ISpecification<T>>.OrderBy(Expression<Func<T, object>> orderByExpression)
    {
        return OrderBy(orderByExpression);
    }

    ISpecification<T> IBaseSpecification<T, ISpecification<T>>.OrderByDesc(Expression<Func<T, object>> orderByDescExpression)
    {
        return OrderByDesc(orderByDescExpression);
    }

    public static Specification<T> Create()
    {
        return new Specification<T>();
    }

    protected override Specification<T> AsTSpecification()
    {
        return this;
    }
}