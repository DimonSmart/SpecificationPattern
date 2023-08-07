using System.Linq.Expressions;

namespace DimonSmart.Specification.EntityFrameworkCore;

public class EFCoreSpecification<T> : BaseSpecification<T, EFCoreSpecification<T>>, IEFCoreSpecification<T>
    where T : class
{
    private List<string> Includes { get; } = new();
    public string CurrentIncludeLevel { get; private set; } = string.Empty;
    public bool IsAsNoTracking { get; private set; }
    public bool IsAsNoTrackingWithIdentityResolution { get; private set; }
    public bool IsIgnoreAutoIncludes { get; private set; }
    public bool IsIgnoreQueryFilters { get; private set; }

    public void AddInclude(string include)
    {
        include = string.IsNullOrEmpty(CurrentIncludeLevel) ? include : $"{CurrentIncludeLevel}.{include}";
        CurrentIncludeLevel = include;
        if (CanBeExcluded(include))
        {
            return;
        }

        Includes.Add(include);
    }

    public IReadOnlyCollection<string> GetIncludes()
    {
        return Includes;
    }

    public IEFCoreSpecification<T> IgnoreAutoIncludes()
    {
        IsIgnoreAutoIncludes = true;
        return this;
    }

    public IEFCoreSpecification<T> IgnoreQueryFilters()
    {
        IsIgnoreQueryFilters = true;
        return this;
    }

    public IEFCoreSpecification<T> AsNoTracking()
    {
        IsAsNoTracking = true;
        return this;
    }

    public IEFCoreSpecification<T> AsNoTrackingWithIdentityResolution()
    {
        IsAsNoTrackingWithIdentityResolution = true;
        return this;
    }

    public IEFCoreSpecification<T> Or(IEFCoreSpecification<T> or)
    {
        Includes.AddRange(or.GetIncludes());
        Or(or.WhereExpression);
        return this;
    }

    public IEFCoreSpecification<T> And(IEFCoreSpecification<T> and)
    {
        Includes.AddRange(and.GetIncludes());
        And(and.WhereExpression);
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

    public bool CanBeExcluded(string newLine)
    {
        var newLineWithDot = newLine + ".";
        foreach (var existingLine in Includes)
        {
            var existingLineWithDot = existingLine + ".";

            if (existingLineWithDot.StartsWith(newLineWithDot))
            {
                return true;
            }
        }

        return false;
    }

    protected override EFCoreSpecification<T> AsTSpecification()
    {
        return this;
    }
}