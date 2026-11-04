using System.Collections.Immutable;

namespace DimonSmart.Specification.EntityFrameworkCore;

/// <summary>Read-only diagnostic snapshot. It is never used to construct an EF query.</summary>
public sealed class EFCoreSpecificationData<T> : IEFCoreSpecificationData<T> where T : class
{
    private readonly QueryOptions<T> _options;
    private readonly ImmutableArray<string> _paths;

    internal EFCoreSpecificationData(QueryOptions<T> options)
    {
        _options = options;
        _paths = options.Includes
            .Select(plan => string.Join(".", plan.Segments.Select(segment => segment.Name)))
            .ToImmutableArray();
    }

    public bool IsAsNoTracking => _options.Tracking == TrackingMode.NoTracking;
    public bool IsAsNoTrackingWithIdentityResolution => _options.Tracking == TrackingMode.NoTrackingWithIdentityResolution;
    public bool IsIgnoreAutoIncludes => _options.IgnoreAutoIncludes;
    public bool IsIgnoreQueryFilters => _options.QueryFilters.Mode == QueryFilterMode.All;
    public IReadOnlyCollection<string> IgnoredQueryFilterNames => _options.QueryFilters.Names;
    public bool IsAsSingleQuery => _options.Splitting == QuerySplittingMode.Single;
    public bool IsAsSplitQuery => _options.Splitting == QuerySplittingMode.Split;
    public IEnumerable<string> Includes => _paths;
    public string Tag => string.Join(Environment.NewLine, _options.Tags);
    public IEnumerable<string> Tags => _options.Tags;
}
