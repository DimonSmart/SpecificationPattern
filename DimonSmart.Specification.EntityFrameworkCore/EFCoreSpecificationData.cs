namespace DimonSmart.Specification.EntityFrameworkCore;

public class EFCoreSpecificationData<T> : IEFCoreSpecificationData<T>
{
    private readonly List<string> _includeLines = new();
    public bool IsAsNoTracking { get; set; }
    public bool IsAsNoTrackingWithIdentityResolution { get; set; }
    public bool IsIgnoreAutoIncludes { get; set; }
    public bool IsIgnoreQueryFilters { get; set; }
    public IEnumerable<string> Includes => _includeLines;

    public void AddInclude(string include)
    {
        if (_includeLines.Contains(include))
        {
            return;
        }

        var includeWithDot = include + ".";
        var shorterLines = _includeLines.FindAll(line => line.StartsWith(includeWithDot));
        _includeLines.RemoveAll(line => shorterLines.Contains(line) || line.StartsWith(includeWithDot));
        _includeLines.Add(include);
    }
}