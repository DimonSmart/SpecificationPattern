using System.Collections.Immutable;

namespace DimonSmart.Specification.EntityFrameworkCore;

internal enum TrackingMode { Default, Tracking, NoTracking, NoTrackingWithIdentityResolution }
internal enum QuerySplittingMode { Default, Single, Split }

/// <summary>Immutable EF-only state; no diagnostic string is used to execute a query.</summary>
internal sealed record QueryOptions<TEntity> where TEntity : class
{
    internal TrackingMode Tracking { get; init; }
    internal QuerySplittingMode Splitting { get; init; }
    internal bool IgnoreAutoIncludes { get; init; }
    internal QueryFilterOptions QueryFilters { get; init; } = QueryFilterOptions.Default;
    internal ImmutableArray<string> Tags { get; init; } = ImmutableArray<string>.Empty;
    internal ImmutableArray<IIncludePlan<TEntity>> Includes { get; init; } = ImmutableArray<IIncludePlan<TEntity>>.Empty;

    internal QueryOptions<TEntity> AddInclude(IIncludePlan<TEntity> plan, string operation) =>
        this with { Includes = IncludePlanMerger<TEntity>.Add(Includes, plan, operation) };

    internal QueryOptions<TEntity> ReplaceInclude(
        IIncludePlan<TEntity> previous, IIncludePlan<TEntity> next)
    {
        // The active plan might already have been absorbed by a longer independent branch.
        // In that case, nothing is removed and the new plan is merged normally.
        var remaining = Includes.Where(p => !ReferenceEquals(p, previous)).ToImmutableArray();
        return this with { Includes = IncludePlanMerger<TEntity>.Add(remaining, next, "ThenInclude") };
    }

    internal QueryOptions<TEntity> AddTag(string tag) =>
        string.IsNullOrWhiteSpace(tag) ? this : this with { Tags = Tags.Add(tag) };

    internal QueryOptions<TEntity> Merge(QueryOptions<TEntity> other, string operation)
    {
        if (Tracking != TrackingMode.Default && other.Tracking != TrackingMode.Default &&
            Tracking != other.Tracking)
            throw new InvalidOperationException("Cannot combine specifications using " + operation + ": conflicting tracking modes (" + Tracking + " vs " + other.Tracking + ").");
        if (Splitting != QuerySplittingMode.Default && other.Splitting != QuerySplittingMode.Default &&
            Splitting != other.Splitting)
            throw new InvalidOperationException("Cannot combine specifications using " + operation + ": conflicting query splitting modes (" + Splitting + " vs " + other.Splitting + ").");

        var plans = Includes;
        foreach (var plan in other.Includes)
            plans = IncludePlanMerger<TEntity>.Add(plans, plan, operation);

        return this with
        {
            Includes = plans,
            Tracking = Tracking == TrackingMode.Default ? other.Tracking : Tracking,
            Splitting = Splitting == QuerySplittingMode.Default ? other.Splitting : Splitting,
            IgnoreAutoIncludes = IgnoreAutoIncludes || other.IgnoreAutoIncludes,
            QueryFilters = QueryFilters.Merge(other.QueryFilters),
            Tags = Tags.AddRange(other.Tags)
        };
    }
}
