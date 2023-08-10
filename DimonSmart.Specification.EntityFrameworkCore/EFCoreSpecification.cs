using System.Linq.Expressions;

namespace DimonSmart.Specification.EntityFrameworkCore;

public class EFCoreSpecification<T> : BaseSpecification<T, EFCoreSpecification<T>>, IEFCoreSpecification<T>
    where T : class
{
    private readonly EFCoreSpecificationData<T> _efCoreSpecificationData = new();
    public string CurrentIncludeLevel { get; private set; } = string.Empty;
    public IEFCoreSpecificationData<T> EFCoreSpecificationData => _efCoreSpecificationData;

    public void AddInclude(string include, bool resetIncludeLevel)
    {
        if (resetIncludeLevel)
        {
            CurrentIncludeLevel = string.Empty;
        }

        include = string.IsNullOrEmpty(CurrentIncludeLevel) ? include : $"{CurrentIncludeLevel}.{include}";
        CurrentIncludeLevel = include;
        _efCoreSpecificationData.AddInclude(include);
    }

    public IEFCoreSpecification<T> IgnoreAutoIncludes()
    {
        _efCoreSpecificationData.IsIgnoreAutoIncludes = true;
        return this;
    }

    public IEFCoreSpecification<T> IgnoreQueryFilters()
    {
        _efCoreSpecificationData.IsIgnoreQueryFilters = true;
        return this;
    }

    public IEFCoreSpecification<T> AsNoTracking()
    {
        _efCoreSpecificationData.IsAsNoTracking = true;
        return this;
    }

    public IEFCoreSpecification<T> AsNoTrackingWithIdentityResolution()
    {
        _efCoreSpecificationData.IsAsNoTrackingWithIdentityResolution = true;
        return this;
    }

    public IEFCoreSpecification<T> Or(IEFCoreSpecification<T> or)
    {
        foreach (var include in or.EFCoreSpecificationData.Includes)
        {
            _efCoreSpecificationData.AddInclude(include);
        }

        Or(or.SpecificationData.WhereExpression);
        return this;
    }

    public IEFCoreSpecification<T> And(IEFCoreSpecification<T> and)
    {
        foreach (var include in and.EFCoreSpecificationData.Includes)
        {
            _efCoreSpecificationData.AddInclude(include);
        }

        And(and.SpecificationData.WhereExpression);
        return this;
    }

    IEFCoreSpecification<T> IBaseSpecification<T, IEFCoreSpecification<T>>.Where(Expression<Func<T, bool>> expr)
    {
        return Where(expr);
    }

    IEFCoreSpecification<T> IBaseSpecification<T, IEFCoreSpecification<T>>.Take(int take)
    {
        return Take(take);
    }

    IEFCoreSpecification<T> IBaseSpecification<T, IEFCoreSpecification<T>>.Skip(int skip)
    {
        return Skip(skip);
    }

    IEFCoreSpecification<T> IBaseSpecification<T, IEFCoreSpecification<T>>.OrderBy(
        Expression<Func<T, object>> orderByExpression)
    {
        return OrderBy(orderByExpression);
    }

    IEFCoreSpecification<T> IBaseSpecification<T, IEFCoreSpecification<T>>.OrderByDesc(
        Expression<Func<T, object>> orderByDescExpression)
    {
        return OrderByDesc(orderByDescExpression);
    }

    public static IEFCoreSpecification<T> Create()
    {
        return new EFCoreSpecification<T>();
    }

    protected override EFCoreSpecification<T> AsTSpecification()
    {
        return this;
    }
}