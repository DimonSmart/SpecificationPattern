using System.Linq.Expressions;

namespace DimonSmart.Specification.EntityFrameworkCore;

public class EFCoreIncludeSpecification<T, TProperty> : IEFCoreIncludeSpecification<T, TProperty> where T : class
{
    private readonly IEFCoreSpecification<T> _parentSpecification;

    public EFCoreIncludeSpecification(IEFCoreSpecification<T> parentSpecification)
    {
        _parentSpecification = parentSpecification;
    }

    public IEFCoreSpecification<T> Where(Expression<Func<T, bool>> expr)
    {
        return _parentSpecification.Where(expr);
    }

    public ISpecificationData<T> SpecificationData =>
        _parentSpecification.SpecificationData;

    public Expression<Func<T, bool>>? GetWhereExpression()
    {
        return _parentSpecification.GetWhereExpression();
    }

    public IEFCoreSpecification<T> Take(int take)
    {
        return _parentSpecification.Take(take);
    }

    public IEFCoreSpecification<T> Skip(int skip)
    {
        return _parentSpecification.Skip(skip);
    }

    public IEFCoreSpecification<T> OrderBy(Expression<Func<T, object>> orderByExpression)
    {
        return _parentSpecification.OrderBy(orderByExpression);
    }

    public IEFCoreSpecification<T> OrderByDesc(Expression<Func<T, object>> orderByDescExpression)
    {
        return _parentSpecification.OrderByDesc(orderByDescExpression);
    }

    public bool IsAsNoTracking =>
        _parentSpecification.IsAsNoTracking;

    public bool IsAsNoTrackingWithIdentityResolution =>
        _parentSpecification.IsAsNoTrackingWithIdentityResolution;

    public bool IsIgnoreAutoIncludes =>
        _parentSpecification.IsIgnoreAutoIncludes;

    public bool IsIgnoreQueryFilters =>
        _parentSpecification.IsIgnoreQueryFilters;

    public IEFCoreSpecification<T> AsNoTracking()
    {
        return _parentSpecification.AsNoTracking();
    }

    public IEFCoreSpecification<T> AsNoTrackingWithIdentityResolution()
    {
        return _parentSpecification.AsNoTrackingWithIdentityResolution();
    }

    public IEFCoreSpecification<T> IgnoreAutoIncludes()
    {
        return _parentSpecification.IgnoreAutoIncludes();
    }

    public IEFCoreSpecification<T> IgnoreQueryFilters()
    {
        return _parentSpecification.IgnoreQueryFilters();
    }

    public IEFCoreSpecification<T> Or(IEFCoreSpecification<T> or)
    {
        return _parentSpecification.Or(or);
    }

    public IEFCoreSpecification<T> And(IEFCoreSpecification<T> and)
    {
        return _parentSpecification.And(and);
    }

    public void AddInclude(string include)
    {
        _parentSpecification.AddInclude(include);
    }

    public IReadOnlyCollection<string> GetIncludes()
    {
        return _parentSpecification.GetIncludes();
    }
}