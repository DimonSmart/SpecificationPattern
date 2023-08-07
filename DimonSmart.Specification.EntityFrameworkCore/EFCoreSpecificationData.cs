namespace DimonSmart.Specification.EntityFrameworkCore;

public class EFCoreSpecificationData<T> : IEFCoreSpecificationData<T>
{
    public bool IsAsNoTracking { get; set; }
    public bool IsAsNoTrackingWithIdentityResolution { get; set; }
    public bool IsIgnoreAutoIncludes { get; set; }
    public bool IsIgnoreQueryFilters { get; set; }
}