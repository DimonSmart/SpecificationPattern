namespace DimonSmart.Specification.EntityFrameworkCore;

public interface IEFCoreSpecification<T> : IBaseSpecification<T, IEFCoreSpecification<T>> where T : class
{
    bool IsAsNoTracking { get; }
    bool IsAsNoTrackingWithIdentityResolution { get; }
    bool IsIgnoreAutoIncludes { get; }
    bool IsIgnoreQueryFilters { get; }
    void AddInclude(string include);
    public IReadOnlyCollection<string> GetIncludes();
    IEFCoreSpecification<T> AsNoTracking();
    IEFCoreSpecification<T> AsNoTrackingWithIdentityResolution();
    IEFCoreSpecification<T> IgnoreAutoIncludes();
    IEFCoreSpecification<T> IgnoreQueryFilters();
    IEFCoreSpecification<T> Or(IEFCoreSpecification<T> or);
    IEFCoreSpecification<T> And(IEFCoreSpecification<T> and);
}