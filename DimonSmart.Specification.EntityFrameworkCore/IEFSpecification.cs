namespace DimonSmart.Specification.EntityFrameworkCore;

public interface IEFSpecification<T> : IBaseSpecification<T, IEFSpecification<T>> where T : class
{
    bool IsAsNoTracking { get; }

    bool IsIgnoreAutoIncludes { get; }
    void AddInclude(string include);
    public IReadOnlyCollection<string> GetIncludes();
    IEFSpecification<T> AsNoTracking();
    IEFSpecification<T> IgnoreAutoIncludes();

    IEFSpecification<T> Or(IEFSpecification<T> or);

    IEFSpecification<T> And(IEFSpecification<T> and);
}