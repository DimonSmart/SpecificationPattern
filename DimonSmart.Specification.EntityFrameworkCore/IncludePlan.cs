using System.Collections.Immutable;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace DimonSmart.Specification.EntityFrameworkCore;

internal interface IIncludePlan<TEntity> where TEntity : class
{
    ImmutableArray<IncludeSegment> Segments { get; }
    IQueryable<TEntity> Apply(IQueryable<TEntity> query);
}

// Covariance keeps List<T> and filtered IEnumerable<T> compatible with EF Core's
// collection ThenInclude overload without reflection or untyped delegates.
internal interface ITypedIncludePlan<TEntity, out TProperty> : IIncludePlan<TEntity>
    where TEntity : class
{
    IIncludableQueryable<TEntity, TProperty> ApplyTyped(IQueryable<TEntity> query);
}

internal sealed class IncludeChain<TEntity, TProperty> : ITypedIncludePlan<TEntity, TProperty>
    where TEntity : class
{
    private readonly Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, TProperty>> _apply;

    internal IncludeChain(
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, TProperty>> apply,
        IEnumerable<IncludeSegment> segments)
    {
        _apply = apply;
        Segments = segments.ToImmutableArray();
    }

    public ImmutableArray<IncludeSegment> Segments { get; }

    public IIncludableQueryable<TEntity, TProperty> ApplyTyped(IQueryable<TEntity> query) => _apply(query);
    public IQueryable<TEntity> Apply(IQueryable<TEntity> query) => ApplyTyped(query);

    internal static IncludeChain<TEntity, TProperty> Start(Expression<Func<TEntity, TProperty>> expression) =>
        new(query => query.Include(expression), IncludePath.Parse(expression));
}

/// <summary>EF Core string Include, normalized with typed MemberInfo navigation identities.</summary>
internal sealed class StringIncludePlan<TEntity> : IIncludePlan<TEntity> where TEntity : class
{
    private readonly string _path;

    internal StringIncludePlan(string path, IEnumerable<IncludeSegment> segments)
    {
        _path = path;
        Segments = segments.ToImmutableArray();
    }

    public ImmutableArray<IncludeSegment> Segments { get; }

    public IQueryable<TEntity> Apply(IQueryable<TEntity> query) => query.Include(_path);
}

/// <summary>Centralized, early conflict detection and stable normalization for all Include entry points.</summary>
internal static class IncludePlanMerger<TEntity> where TEntity : class
{
    internal static ImmutableArray<IIncludePlan<TEntity>> Add(
        ImmutableArray<IIncludePlan<TEntity>> existing,
        IIncludePlan<TEntity> candidate,
        string operation)
    {
        foreach (var plan in existing)
            CheckCompatibility(plan.Segments, candidate.Segments, operation);

        // If the candidate is covered by an existing branch, keep the earlier branch.
        if (existing.Any(plan => IsPrefix(candidate.Segments, plan.Segments)))
            return existing;

        var firstShorter = -1;
        var merged = new List<IIncludePlan<TEntity>>();
        foreach (var plan in existing)
        {
            if (IsPrefix(plan.Segments, candidate.Segments))
            {
                if (firstShorter < 0)
                {
                    firstShorter = merged.Count;
                    merged.Add(candidate);
                }
                continue;
            }
            merged.Add(plan);
        }

        if (firstShorter < 0)
            merged.Add(candidate);
        return merged.ToImmutableArray();
    }

    private static bool IsPrefix(ImmutableArray<IncludeSegment> prefix, ImmutableArray<IncludeSegment> full)
    {
        if (prefix.Length > full.Length)
            return false;

        for (var i = 0; i < prefix.Length; i++)
            if (!SameNavigation(prefix[i], full[i]) || prefix[i].FilterId != full[i].FilterId)
                return false;
        return true;
    }

    private static bool SameNavigation(IncludeSegment a, IncludeSegment b) =>
        a.Member.Equals(b.Member);

    private static void CheckCompatibility(
        ImmutableArray<IncludeSegment> left, ImmutableArray<IncludeSegment> right, string operation)
    {
        var path = new List<string>();
        for (var i = 0; i < Math.Min(left.Length, right.Length); i++)
        {
            if (!SameNavigation(left[i], right[i]))
                break;
            path.Add(left[i].Name);

            if (left[i].FilterId == right[i].FilterId)
                continue;

            var reason = left[i].FilterId is null || right[i].FilterId is null
                ? "unfiltered vs filtered"
                : "different filtered includes";
            throw new InvalidOperationException(
                operation + " has conflicting Include for '" + string.Join(".", path) + "': " + reason + ".");
        }
    }
}
