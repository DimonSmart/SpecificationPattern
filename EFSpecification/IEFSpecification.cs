using DimonSmart.Specification;
namespace DimonSmart.EFSpecification;

public interface IEFSpecification<T> : IBaseSpecification<T, IEFSpecification<T>> where T : class
{
    void AddInclude(string include);
    public IReadOnlyCollection<string> GetIncludes();

    bool IsAsNoTracking { get; }
    IEFSpecification<T> AsNoTracking();

    bool IsIgnoreAutoIncludes { get; }
    IEFSpecification<T> IgnoreAutoIncludes();

    IEFSpecification<T> Or(IEFSpecification<T> or);

    IEFSpecification<T> And(IEFSpecification<T> and);
}