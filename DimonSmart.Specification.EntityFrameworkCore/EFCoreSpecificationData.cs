namespace DimonSmart.Specification.EntityFrameworkCore;

public class EFCoreSpecificationData<T> : IEFCoreSpecificationData<T>
{
    private readonly List<string> _includeLines = new();
    public bool IsAsNoTracking { get; set; }
    public bool IsAsNoTrackingWithIdentityResolution { get; set; }
    public bool IsIgnoreAutoIncludes { get; set; }
    public bool IsIgnoreQueryFilters { get; set; }
    public bool IsAsSingleQuery { get; set; }
    public bool IsAsSplitQuery { get; set; }
    public IEnumerable<string> Includes => _includeLines;
    public string Tag { get; set; } = string.Empty;

    public void AddInclude(string include)
    {
        if (string.IsNullOrWhiteSpace(include))
        {
            throw new ArgumentException("Include cannot be null or empty.", nameof(include));
        }

        var includeWithDot = include + ".";
        if (_includeLines.Contains(include) || _includeLines.Any(i => i.StartsWith(includeWithDot)))
        {
            // Already included or overlapping
            return;
        }

        var shorterLines = _includeLines
            .FindAll(line => includeWithDot.StartsWith(line + "."))
            .ToList();
        _includeLines.RemoveAll(line => shorterLines.Contains(line));
        _includeLines.Add(include);
    }
}