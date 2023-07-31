using System.Linq.Expressions;
using DimonSmart.Specification;

namespace DimonSmart.EFSpecification;

public class EFSpecification<T> : BaseSpecification<T, EFSpecification<T>>, IEFSpecification<T> where T : class
{
    private List<string> Includes { get; } = new();
    public string CurrentIncludeLevel { get; private set; } = string.Empty;
    public bool IsAsNoTracking { get; private set; }
    public bool IsIgnoreAutoIncludes { get; private set; }

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

    public IEFSpecification<T> IgnoreAutoIncludes()
    {
        IsIgnoreAutoIncludes = true;
        return this;
    }

    public IEFSpecification<T> AsNoTracking()
    {
        IsAsNoTracking = true;
        return this;
    }

    public IEFSpecification<T> Or(IEFSpecification<T> or)
    {
        Includes.AddRange(or.GetIncludes());
        Or(or.WhereExpression);
        return this;
    }

    public IEFSpecification<T> And(IEFSpecification<T> and)
    {
        Includes.AddRange(and.GetIncludes());
        And(and.WhereExpression);
        return this;
    }

    IEFSpecification<T> IBaseSpecification<T, IEFSpecification<T>>.Where(Expression<Func<T, bool>> expr)
    {
        return Where(expr);
    }

    IEFSpecification<T> IBaseSpecification<T, IEFSpecification<T>>.OrderBy(
        Expression<Func<T, object>> orderByExpression)
    {
        return OrderBy(orderByExpression);
    }

    IEFSpecification<T> IBaseSpecification<T, IEFSpecification<T>>.OrderByDesc(
        Expression<Func<T, object>> orderByDescExpression)
    {
        return OrderByDesc(orderByDescExpression);
    }

    public static IEFSpecification<T> Create()
    {
        return new EFSpecification<T>();
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

    protected override EFSpecification<T> AsTSpecification()
    {
        return this;
    }
}