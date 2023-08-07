using System.Linq.Expressions;

namespace DimonSmart.Specification.EntityFrameworkCore;

public class EFCoreIncludeSpecification<T, TProperty> : IEFCoreIncludeSpecification<T, TProperty> where T : class
{
    private readonly IEFCoreSpecification<T> _parentSpecification;

    public EFCoreIncludeSpecification(IEFCoreSpecification<T> parentSpecification)
    {
        _parentSpecification = parentSpecification;
    }
   
    public IEFCoreSpecification<T> Where(Expression<Func<T, bool>> expr) =>
        _parentSpecification.Where(expr);

    public ISpecificationData<T> SpecificationData =>
        _parentSpecification.SpecificationData;

    public Expression<Func<T, bool>>? GetWhereExpression() =>
        _parentSpecification.GetWhereExpression();

    public IEFCoreSpecification<T> Take(int take) =>
        _parentSpecification.Take(take);

    public IEFCoreSpecification<T> Skip(int skip) =>
        _parentSpecification.Skip(skip);

    public IEFCoreSpecification<T> OrderBy(Expression<Func<T, object>> orderByExpression) =>
        _parentSpecification.OrderBy(orderByExpression);

    public IEFCoreSpecification<T> OrderByDesc(Expression<Func<T, object>> orderByDescExpression) =>
        _parentSpecification.OrderByDesc(orderByDescExpression);

    public bool IsAsNoTracking =>
        _parentSpecification.IsAsNoTracking;

    public bool IsAsNoTrackingWithIdentityResolution =>
        _parentSpecification.IsAsNoTrackingWithIdentityResolution;

    public bool IsIgnoreAutoIncludes =>
        _parentSpecification.IsIgnoreAutoIncludes;

    public bool IsIgnoreQueryFilters =>
        _parentSpecification.IsIgnoreQueryFilters;

    public IEFCoreSpecification<T> AsNoTracking() =>
        _parentSpecification.AsNoTracking();

    public IEFCoreSpecification<T> AsNoTrackingWithIdentityResolution() =>
        _parentSpecification.AsNoTrackingWithIdentityResolution();

    public IEFCoreSpecification<T> IgnoreAutoIncludes() =>
        _parentSpecification.IgnoreAutoIncludes();

    public IEFCoreSpecification<T> IgnoreQueryFilters() =>
        _parentSpecification.IgnoreQueryFilters();

    public IEFCoreSpecification<T> Or(IEFCoreSpecification<T> or) =>
        _parentSpecification.Or(or);

    public IEFCoreSpecification<T> And(IEFCoreSpecification<T> and) =>
        _parentSpecification.And(and);

    public void AddInclude(string include) =>
        _parentSpecification.AddInclude(include);

    public IReadOnlyCollection<string> GetIncludes() =>
        _parentSpecification.GetIncludes();
}