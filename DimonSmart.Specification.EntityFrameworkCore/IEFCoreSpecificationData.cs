namespace DimonSmart.Specification.EntityFrameworkCore;

public interface IEFCoreSpecificationData<T>
{
    bool IsAsNoTracking { get; }
    bool IsAsNoTrackingWithIdentityResolution { get; }
    bool IsIgnoreAutoIncludes { get; }
    bool IsIgnoreQueryFilters { get; }
}