using System.Collections.Immutable;

namespace DimonSmart.Specification.EntityFrameworkCore;

internal enum QueryFilterMode { Default, Named, All }

/// <summary>Immutable query-filter selection with exactly one of Default, Named, or All.</summary>
internal sealed class QueryFilterOptions
{
    internal static QueryFilterOptions Default { get; } =
        new(QueryFilterMode.Default, ImmutableArray<string>.Empty);

    internal static QueryFilterOptions All { get; } =
        new(QueryFilterMode.All, ImmutableArray<string>.Empty);

    private QueryFilterOptions(QueryFilterMode mode, ImmutableArray<string> names)
    {
        Mode = mode;
        Names = names;
    }

    internal QueryFilterMode Mode { get; }
    internal ImmutableArray<string> Names { get; }

    internal QueryFilterOptions AddNames(IEnumerable<string> names)
    {
        if (Mode == QueryFilterMode.All)
            return All;

        var result = Names;
        var seen = new HashSet<string>(result, StringComparer.Ordinal);
        foreach (var name in names)
        {
            if (seen.Add(name))
                result = result.Add(name);
        }

        return result.IsEmpty ? this : new QueryFilterOptions(QueryFilterMode.Named, result);
    }

    internal QueryFilterOptions Merge(QueryFilterOptions other)
    {
        if (Mode == QueryFilterMode.All || other.Mode == QueryFilterMode.All)
            return All;
        if (Mode == QueryFilterMode.Default)
            return other;
        if (other.Mode == QueryFilterMode.Default)
            return this;

        return AddNames(other.Names);
    }
}
