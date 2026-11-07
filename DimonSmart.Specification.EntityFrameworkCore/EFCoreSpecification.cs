using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;

namespace DimonSmart.Specification.EntityFrameworkCore;

/// <summary>Immutable EF Core specification with validated query options.</summary>
public class EFCoreSpecification<T> : IEFCoreSpecification<T>
    where T : class
{
    internal Specification<T> Core { get; }
    internal QueryOptions<T> Options { get; }

    internal EFCoreSpecification(Specification<T> core, QueryOptions<T> options)
    {
        Core = core;
        Options = options;
    }

    /// <summary>Initializes a named specification from an existing immutable EF Core specification.</summary>
    protected EFCoreSpecification(EFCoreSpecification<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        Core = source.Core;
        Options = source.Options;
    }

    public ISpecificationData<T> SpecificationData => Core.SpecificationData;
    public IReadOnlyList<QueryPipelineStep> PipelineSteps => Core.PipelineSteps;

    /// <summary>
    /// Tests in-memory LINQ criteria only. EF-only options and global filters do not apply,
    /// and provider-specific expressions may not execute outside EF Core.
    /// </summary>
    public bool IsSatisfiedBy(T candidate) => Core.IsSatisfiedBy(candidate);

    IQueryable<T> ISpecification<T, T>.ApplyTo(IQueryable<T> query) =>
        query.BySpecification(this);

    IQueryable<T> ISpecification<T, T>.ApplyCriteriaTo(IQueryable<T> query) =>
        query.BySpecificationCriteria(this);

    // Expose diagnostics as a snapshot. Query execution uses Options, never this projection.
    public IEFCoreSpecificationData<T> EFCoreSpecificationData => new EFCoreSpecificationData<T>(Options);

    /// <summary>Creates a terminal projection; explicit Include plans are incompatible with Select.</summary>
    public EFCoreProjectedSpecification<T, TResult> Select<TResult>(Expression<Func<T, TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        if (!Options.Includes.IsEmpty)
            throw new InvalidOperationException(
                "Select cannot be combined with explicit Include/ThenInclude plans. Project navigations directly in the selector.");

        return new EFCoreProjectedSpecification<T, TResult>(Core.Select(selector), Options);
    }

    public EFCoreProjectedSpecification<T, TCollection> SelectMany<TCollection>(
        Expression<Func<T, IEnumerable<TCollection>>> collectionSelector)
    {
        ArgumentNullException.ThrowIfNull(collectionSelector);
        EnsureNoIncludeForProjection("SelectMany");
        return new(Core.SelectMany(collectionSelector), Options);
    }

    public EFCoreProjectedSpecification<T, TResult> SelectMany<TCollection, TResult>(
        Expression<Func<T, IEnumerable<TCollection>>> collectionSelector,
        Expression<Func<T, TCollection, TResult>> resultSelector)
    {
        ArgumentNullException.ThrowIfNull(collectionSelector);
        ArgumentNullException.ThrowIfNull(resultSelector);
        EnsureNoIncludeForProjection("SelectMany");
        return new(Core.SelectMany(collectionSelector, resultSelector), Options);
    }

    /// <summary>Appends an existing typed projection and merges its root EF options.</summary>
    public EFCoreProjectedSpecification<T, TResult> WithProjectionOf<TResult>(
        EFCoreProjectedSpecification<T, TResult> projection)
    {
        ArgumentNullException.ThrowIfNull(projection);
        if (!Options.Includes.IsEmpty || !projection.Options.Includes.IsEmpty)
            throw new InvalidOperationException(
                "WithProjectionOf cannot be combined with explicit Include/ThenInclude plans.");
        return new(Core.WithProjectionOf(projection.Projection),
            Options.Merge(projection.Options, "WithProjectionOf"));
    }

    private void EnsureNoIncludeForProjection(string operation)
    {
        if (!Options.Includes.IsEmpty)
            throw new InvalidOperationException(
                operation + " cannot be combined with explicit Include/ThenInclude plans. " +
                "Project navigations directly in the selector.");
    }

    public EFCoreSpecification<T> Where(Expression<Func<T, bool>> predicate) =>
        new(Core.Where(predicate), Options);

    /// <summary>Adds a filter only when <paramref name="condition"/> is true.</summary>
    public EFCoreSpecification<T> Where(Expression<Func<T, bool>> predicate, bool condition)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return condition ? Where(predicate) : this;
    }

    public EFCoreSpecification<T> OrderBy<TKey>(Expression<Func<T, TKey>> expression) =>
        new(Core.OrderBy(expression), Options);

    public EFCoreSpecification<T> OrderBy<TKey>(Expression<Func<T, TKey>> expression, bool condition) =>
        WithConditionalCore(Core.OrderBy(expression, condition), condition);

    public EFCoreSpecification<T> OrderByDesc<TKey>(Expression<Func<T, TKey>> expression) =>
        new(Core.OrderByDesc(expression), Options);

    public EFCoreSpecification<T> OrderByDesc<TKey>(Expression<Func<T, TKey>> expression, bool condition) =>
        WithConditionalCore(Core.OrderByDesc(expression, condition), condition);

    public EFCoreSpecification<T> ThenBy<TKey>(Expression<Func<T, TKey>> expression) =>
        new(Core.ThenBy(expression), Options);

    public EFCoreSpecification<T> ThenBy<TKey>(Expression<Func<T, TKey>> expression, bool condition) =>
        WithConditionalCore(Core.ThenBy(expression, condition), condition);

    public EFCoreSpecification<T> ThenByDesc<TKey>(Expression<Func<T, TKey>> expression) =>
        new(Core.ThenByDesc(expression), Options);

    public EFCoreSpecification<T> ThenByDesc<TKey>(Expression<Func<T, TKey>> expression, bool condition) =>
        WithConditionalCore(Core.ThenByDesc(expression, condition), condition);

    public EFCoreSpecification<T> Take(int count) =>
        new(Core.Take(count), Options);

    public EFCoreSpecification<T> Take(int count, bool condition) =>
        WithConditionalCore(Core.Take(count, condition), condition);

    public EFCoreSpecification<T> Skip(int count) =>
        new(Core.Skip(count), Options);

    public EFCoreSpecification<T> Skip(int count, bool condition) =>
        WithConditionalCore(Core.Skip(count, condition), condition);

    private EFCoreSpecification<T> WithConditionalCore(Specification<T> core, bool condition) =>
        condition ? new(core, Options) : this;

    public EFCoreSpecification<T> SeekAfter<TCursor>(TCursor cursor) =>
        new(Core.SeekAfter(cursor), Options);

    /// <summary>Explicitly enables EF Core change tracking, even when the context defaults to NoTracking.</summary>
    public EFCoreSpecification<T> AsTracking() =>
        new(Core, Options with { Tracking = TrackingMode.Tracking });

    public EFCoreSpecification<T> AsTracking(bool condition) =>
        condition ? AsTracking() : this;

    public EFCoreSpecification<T> AsNoTracking() =>
        new(Core, Options with { Tracking = TrackingMode.NoTracking });

    public EFCoreSpecification<T> AsNoTracking(bool condition) =>
        condition ? AsNoTracking() : this;

    public EFCoreSpecification<T> AsNoTrackingWithIdentityResolution() =>
        new(Core, Options with { Tracking = TrackingMode.NoTrackingWithIdentityResolution });

    public EFCoreSpecification<T> AsNoTrackingWithIdentityResolution(bool condition) =>
        condition ? AsNoTrackingWithIdentityResolution() : this;

    public EFCoreSpecification<T> AsSplitQuery() =>
        new(Core, Options with { Splitting = QuerySplittingMode.Split });

    public EFCoreSpecification<T> AsSplitQuery(bool condition) =>
        condition ? AsSplitQuery() : this;

    public EFCoreSpecification<T> AsSingleQuery() =>
        new(Core, Options with { Splitting = QuerySplittingMode.Single });

    public EFCoreSpecification<T> AsSingleQuery(bool condition) =>
        condition ? AsSingleQuery() : this;

    public EFCoreSpecification<T> IgnoreAutoIncludes() =>
        new(Core, Options with { IgnoreAutoIncludes = true });

    public EFCoreSpecification<T> IgnoreAutoIncludes(bool condition) =>
        condition ? IgnoreAutoIncludes() : this;

    /// <summary>Disables all global query filters for the final EF Core query, including tenant filters.</summary>
    public EFCoreSpecification<T> IgnoreQueryFilters() =>
        new(Core, Options with { QueryFilters = QueryFilterOptions.All });

    public EFCoreSpecification<T> IgnoreQueryFilters(bool condition) =>
        condition ? IgnoreQueryFilters() : this;

    /// <summary>
    /// Disables the specified named global query filters for the final EF Core query.
    /// Other filters remain active. This is a query option, not an authorization boundary:
    /// callers must control which names can be disabled, especially in multi-tenant applications.
    /// Repeated calls accumulate names; a preceding full IgnoreQueryFilters() always takes precedence.
    /// </summary>
    public EFCoreSpecification<T> IgnoreQueryFilters(
        string filterName, params string[] additionalFilterNames)
    {
        ArgumentNullException.ThrowIfNull(filterName);
        if (string.IsNullOrWhiteSpace(filterName))
            throw new ArgumentException("Filter name cannot be empty or whitespace.", nameof(filterName));
        ArgumentNullException.ThrowIfNull(additionalFilterNames);
        if (additionalFilterNames.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Filter names cannot be null, empty or whitespace.", nameof(additionalFilterNames));

        return new(Core, Options with
        {
            QueryFilters = Options.QueryFilters.AddNames(
                new[] { filterName }.Concat(additionalFilterNames))
        });
    }

    public EFCoreSpecification<T> TagWith(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        return new(Core, Options.AddTag(tag));
    }

    public EFCoreSpecification<T> TagWith(string tag, bool condition)
    {
        ArgumentNullException.ThrowIfNull(tag);
        return condition ? TagWith(tag) : this;
    }

    public EFCoreSpecification<T> TagWithCallSite(
        string tag = "",
        [CallerMemberName] string callerMemberName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        ArgumentNullException.ThrowIfNull(tag);
        var callSite = callerMemberName + "() -> " + callerFilePath + ":" + callerLineNumber;
        return new(Core, Options.AddTag(tag).AddTag(callSite));
    }


    public EFCoreIncludeSpecification<T, TProperty> Include<TProperty>(
        Expression<Func<T, TProperty>> expression) => Include(expression, true);

    /// <summary>Conditionally starts a typed Include chain; a disabled chain ignores all ThenInclude steps.</summary>
    public EFCoreIncludeSpecification<T, TProperty> Include<TProperty>(
        Expression<Func<T, TProperty>> expression, bool condition)
    {
        ArgumentNullException.ThrowIfNull(expression);
        if (!condition)
            return new(Core, Options, null);
        var plan = IncludeChain<T, TProperty>.Start(expression);
        return new(Core, Options.AddInclude(plan, "Include"), plan);
    }

    internal IIncludePlan<T>? GetActivePlan() =>
        this is IActiveInclude<T> active
            ? active.ActivePlan
            : throw new InvalidOperationException("ThenInclude requires an active Include chain.");

    internal EFCoreIncludeSpecification<T, TProperty> AppendThenInclude<TProperty>(
        IIncludePlan<T> previous, IIncludePlan<T> next) =>
        new(Core, Options.ReplaceInclude(previous, next), next);


    public void EnsureCompatibleWith(EFCoreSpecification<T> other) =>
        EnsureCompatibleWith(other, "EnsureCompatibleWith");

    private void EnsureCompatibleWith(EFCoreSpecification<T> other, string operation)
    {
        ArgumentNullException.ThrowIfNull(other);
        Core.EnsureCompatibleWith(other.Core, operation);
        _ = Options.Merge(other.Options, operation);
    }

    internal EFCoreSpecification<T> Combine(EFCoreSpecification<T> other, bool and)
    {
        var operation = and ? "And" : "Or";
        EnsureCompatibleWith(other, operation);
        return new EFCoreSpecification<T>(Core.Combine(other.Core, and), Options.Merge(other.Options, operation));
    }

}

public static class EFCoreSpecification
{
    public static EFCoreSpecification<T> Create<T>() where T : class =>
        new(Specification.Create<T>(), new QueryOptions<T>());
}

/// <summary>Typed active Include result, still usable as an ordinary specification.</summary>
public interface IEFCoreIncludeSpecification<T, out TProperty>
    where T : class
{
    EFCoreSpecification<T> Specification { get; }
    string IncludePath { get; }
}

internal interface IActiveInclude<T> where T : class
{
    IIncludePlan<T>? ActivePlan { get; }
}

public sealed class EFCoreIncludeSpecification<T, TProperty> :
    EFCoreSpecification<T>, IEFCoreIncludeSpecification<T, TProperty>, IActiveInclude<T>
    where T : class
{
    internal EFCoreIncludeSpecification(
        Specification<T> core, QueryOptions<T> options, IIncludePlan<T>? plan)
        : base(core, options)
    {
        ActivePlan = plan;
        IncludePath = plan is null ? string.Empty :
            string.Join(".", plan.Segments.Select(segment => segment.Name));
    }

    IIncludePlan<T>? IActiveInclude<T>.ActivePlan => ActivePlan;
    internal IIncludePlan<T>? ActivePlan { get; }

    public EFCoreSpecification<T> Specification => this;
    public string IncludePath { get; }
}

public static class EFCoreIncludeExtensions
{
    public static EFCoreIncludeSpecification<T, TProperty> ThenInclude<T, TPrevious, TProperty>(
        this IEFCoreIncludeSpecification<T, IEnumerable<TPrevious>> specification,
        Expression<Func<TPrevious, TProperty>> expression)
        where T : class => ThenInclude(specification, expression, true);

    public static EFCoreIncludeSpecification<T, TProperty> ThenInclude<T, TPrevious, TProperty>(
        this IEFCoreIncludeSpecification<T, IEnumerable<TPrevious>> specification,
        Expression<Func<TPrevious, TProperty>> expression, bool condition)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(expression);
        var source = specification.Specification;
        if (!condition)
            return new(source.Core, source.Options, null);
        var active = source.GetActivePlan();
        if (active is null)
            return new(source.Core, source.Options, null);
        if (active is not ITypedIncludePlan<T, IEnumerable<TPrevious>> previous)
            throw new InvalidOperationException("Active Include is not a compatible collection navigation.");

        var plan = new IncludeChain<T, TProperty>(
            query => previous.ApplyTyped(query).ThenInclude(expression),
            active.Segments.Concat(IncludePath.Parse(expression)));
        return source.AppendThenInclude<TProperty>(active, plan);
    }

    public static EFCoreIncludeSpecification<T, TProperty> ThenInclude<T, TPrevious, TProperty>(
        this IEFCoreIncludeSpecification<T, TPrevious> specification,
        Expression<Func<TPrevious, TProperty>> expression)
        where T : class => ThenInclude(specification, expression, true);

    public static EFCoreIncludeSpecification<T, TProperty> ThenInclude<T, TPrevious, TProperty>(
        this IEFCoreIncludeSpecification<T, TPrevious> specification,
        Expression<Func<TPrevious, TProperty>> expression, bool condition)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(expression);
        var source = specification.Specification;
        if (!condition)
            return new(source.Core, source.Options, null);
        var active = source.GetActivePlan();
        if (active is null)
            return new(source.Core, source.Options, null);
        if (active is not ITypedIncludePlan<T, TPrevious> previous)
            throw new InvalidOperationException("Active Include is not a compatible reference navigation.");

        var plan = new IncludeChain<T, TProperty>(
            query => previous.ApplyTyped(query).ThenInclude(expression),
            active.Segments.Concat(IncludePath.Parse(expression)));
        return source.AppendThenInclude<TProperty>(active, plan);
    }
}

public static class EFCoreCompositionExtensions
{
    public static EFCoreSpecification<T> And<T>(this EFCoreSpecification<T> left, EFCoreSpecification<T> right) where T : class =>
        left.Combine(right, true);

    public static EFCoreSpecification<T> Or<T>(this EFCoreSpecification<T> left, EFCoreSpecification<T> right) where T : class =>
        left.Combine(right, false);
}
