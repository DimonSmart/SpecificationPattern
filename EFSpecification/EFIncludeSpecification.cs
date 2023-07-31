using System.Linq.Expressions;

namespace DimonSmart.EFSpecification;

public class EFIncludeSpecification<T, TProperty> : IEFIncludeSpecification<T, TProperty> where T : class
{
    private readonly IEFSpecification<T> _parentSpecification;

    public EFIncludeSpecification(IEFSpecification<T> parentSpecification)
    {
        _parentSpecification = parentSpecification;
    }

    public Expression<Func<T, bool>>? WhereExpression => _parentSpecification.WhereExpression;

    public IEFSpecification<T> Where(Expression<Func<T, bool>> expr)
    {
        return _parentSpecification.Where(expr);
    }

    public IEFSpecification<T> OrderBy(Expression<Func<T, object>> orderByExpression)
    {
        return _parentSpecification.OrderBy(orderByExpression);
    }

    public IEFSpecification<T> OrderByDesc(Expression<Func<T, object>> orderByDescExpression)
    {
        return _parentSpecification.OrderByDesc(orderByDescExpression);
    }

    public List<(bool direction, Expression<Func<T, object>> expr)> OrderExpressions =>
        _parentSpecification.OrderExpressions;

    public int? TakeQ => _parentSpecification.TakeQ;

    public int? SkipQ => _parentSpecification.SkipQ;

    public void AddInclude(string include)
    {
        _parentSpecification.AddInclude(include);
    }

    public IReadOnlyCollection<string> GetIncludes()
    {
        return _parentSpecification.GetIncludes();
    }

    public bool IsAsNoTracking => _parentSpecification.IsAsNoTracking;

    public IEFSpecification<T> AsNoTracking()
    {
        return _parentSpecification.AsNoTracking();
    }

    public bool IsIgnoreAutoIncludes => _parentSpecification.IsIgnoreAutoIncludes;

    public IEFSpecification<T> IgnoreAutoIncludes()
    {
        return _parentSpecification.IgnoreAutoIncludes();
    }

    public IEFSpecification<T> Or(IEFSpecification<T> or)
    {
        return _parentSpecification.Or(or);
    }

    public IEFSpecification<T> And(IEFSpecification<T> and)
    {
        return _parentSpecification.And(and);
    }
}