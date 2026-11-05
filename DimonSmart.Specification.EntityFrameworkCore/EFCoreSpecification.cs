using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;

namespace DimonSmart.Specification.EntityFrameworkCore;

/// <summary>Immutable EF Core specification with Composable/Configured type-state.</summary>
public class EFCoreSpecification<T, TState>
    where T : class
    where TState : SpecificationState
{
    internal Specification<T, TState> Core { get; }
    internal QueryOptions<T> Options { get; }

    internal EFCoreSpecification(Specification<T, TState> core, QueryOptions<T> options)
    {
        Core = core;
        Options = options;
    }

    public ISpecificationData<T> SpecificationData => Core.SpecificationData;

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

    public EFCoreSpecification<T, TState> Where(Expression<Func<T, bool>> predicate) =>
        new(Core.Where(predicate), Options);

    /// <summary>Adds a filter only when <paramref name="condition"/> is true.</summary>
    public EFCoreSpecification<T, TState> Where(Expression<Func<T, bool>> predicate, bool condition)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return condition ? Where(predicate) : this;
    }

    public EFCoreSpecification<T, Configured> OrderBy<TKey>(Expression<Func<T, TKey>> expression) =>
        new(Core.OrderBy(expression), Options);

    public EFCoreSpecification<T, Configured> OrderBy<TKey>(Expression<Func<T, TKey>> expression, bool condition) =>
        WithConditionalCore(Core.OrderBy(expression, condition), condition);

    public EFCoreSpecification<T, Configured> OrderByDesc<TKey>(Expression<Func<T, TKey>> expression) =>
        new(Core.OrderByDesc(expression), Options);

    public EFCoreSpecification<T, Configured> OrderByDesc<TKey>(Expression<Func<T, TKey>> expression, bool condition) =>
        WithConditionalCore(Core.OrderByDesc(expression, condition), condition);

    public EFCoreSpecification<T, Configured> ThenBy<TKey>(Expression<Func<T, TKey>> expression) =>
        new(Core.ThenBy(expression), Options);

    public EFCoreSpecification<T, Configured> ThenBy<TKey>(Expression<Func<T, TKey>> expression, bool condition) =>
        WithConditionalCore(Core.ThenBy(expression, condition), condition);

    public EFCoreSpecification<T, Configured> ThenByDesc<TKey>(Expression<Func<T, TKey>> expression) =>
        new(Core.ThenByDesc(expression), Options);

    public EFCoreSpecification<T, Configured> ThenByDesc<TKey>(Expression<Func<T, TKey>> expression, bool condition) =>
        WithConditionalCore(Core.ThenByDesc(expression, condition), condition);

    public EFCoreSpecification<T, Configured> Take(int count) =>
        new(Core.Take(count), Options);

    public EFCoreSpecification<T, Configured> Take(int count, bool condition) =>
        WithConditionalCore(Core.Take(count, condition), condition);

    public EFCoreSpecification<T, Configured> Skip(int count) =>
        new(Core.Skip(count), Options);

    public EFCoreSpecification<T, Configured> Skip(int count, bool condition) =>
        WithConditionalCore(Core.Skip(count, condition), condition);

    private EFCoreSpecification<T, Configured> WithConditionalCore(
        Specification<T, Configured> core, bool condition) =>
        !condition && typeof(TState) == typeof(Configured)
            ? (EFCoreSpecification<T, Configured>)(object)this
            : new EFCoreSpecification<T, Configured>(core, Options);

    public EFCoreSpecification<T, Configured> SeekAfter<TCursor>(TCursor cursor) =>
        new(Core.SeekAfter(cursor), Options);

    /// <summary>Explicitly enables EF Core change tracking, even when the context defaults to NoTracking.</summary>
    public EFCoreSpecification<T, Configured> AsTracking() =>
        new(CoreForConfigured(), Options with { Tracking = TrackingMode.Tracking });

    public EFCoreSpecification<T, Configured> AsNoTracking() =>
        new(CoreForConfigured(), Options with { Tracking = TrackingMode.NoTracking });

    public EFCoreSpecification<T, Configured> AsNoTrackingWithIdentityResolution() =>
        new(CoreForConfigured(), Options with { Tracking = TrackingMode.NoTrackingWithIdentityResolution });

    public EFCoreSpecification<T, Configured> AsSplitQuery() =>
        new(CoreForConfigured(), Options with { Splitting = QuerySplittingMode.Split });

    public EFCoreSpecification<T, Configured> AsSingleQuery() =>
        new(CoreForConfigured(), Options with { Splitting = QuerySplittingMode.Single });

    public EFCoreSpecification<T, Configured> IgnoreAutoIncludes() =>
        new(CoreForConfigured(), Options with { IgnoreAutoIncludes = true });

    /// <summary>Disables all global query filters for the final EF Core query, including tenant filters.</summary>
    public EFCoreSpecification<T, Configured> IgnoreQueryFilters() =>
        new(CoreForConfigured(), Options with { QueryFilters = QueryFilterOptions.All });

    /// <summary>
    /// Disables the specified named global query filters for the final EF Core query.
    /// Other filters remain active. This is a query option, not an authorization boundary:
    /// callers must control which names can be disabled, especially in multi-tenant applications.
    /// Repeated calls accumulate names; a preceding full IgnoreQueryFilters() always takes precedence.
    /// </summary>
    public EFCoreSpecification<T, Configured> IgnoreQueryFilters(
        string filterName, params string[] additionalFilterNames)
    {
        ArgumentNullException.ThrowIfNull(filterName);
        if (string.IsNullOrWhiteSpace(filterName))
            throw new ArgumentException("Filter name cannot be empty or whitespace.", nameof(filterName));
        ArgumentNullException.ThrowIfNull(additionalFilterNames);
        if (additionalFilterNames.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Filter names cannot be null, empty or whitespace.", nameof(additionalFilterNames));

        return new(CoreForConfigured(), Options with
        {
            QueryFilters = Options.QueryFilters.AddNames(
                new[] { filterName }.Concat(additionalFilterNames))
        });
    }

    public EFCoreSpecification<T, Configured> TagWith(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        return new(CoreForConfigured(), Options.AddTag(tag));
    }

    public EFCoreSpecification<T, Configured> TagWithCallSite(
        string tag = "",
        [CallerMemberName] string callerMemberName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        ArgumentNullException.ThrowIfNull(tag);
        var callSite = callerMemberName + "() -> " + callerFilePath + ":" + callerLineNumber;
        return new(CoreForConfigured(), Options.AddTag(tag).AddTag(callSite));
    }

    private Specification<T, Configured> CoreForConfigured() =>
        new(Specification<T, TState>.Copy(Core.Data));

    public EFCoreIncludeSpecification<T, TProperty, TState> Include<TProperty>(
        Expression<Func<T, TProperty>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var plan = IncludeChain<T, TProperty>.Start(expression);
        return new(Core, Options.AddInclude(plan, "Include"), plan);
    }

    internal IIncludePlan<T> GetActivePlan() =>
        this is IActiveInclude<T> active
            ? active.ActivePlan
            : throw new InvalidOperationException("ThenInclude requires an active Include chain.");

    internal EFCoreIncludeSpecification<T, TProperty, TState> AppendThenInclude<TProperty>(
        IIncludePlan<T> previous, IIncludePlan<T> next) =>
        new(Core, Options.ReplaceInclude(previous, next), next);

    internal EFCoreSpecification<T, TResult> Combine<TOther, TResult>(
        EFCoreSpecification<T, TOther> other, bool and)
        where TOther : SpecificationState
        where TResult : SpecificationState
    {
        ArgumentNullException.ThrowIfNull(other);

        // Core.Combine retains all existing root-level sorting and paging conflict rules.
        var core = Core.Combine<TOther, TResult>(other.Core, and);
        var options = Options.Merge(other.Options, and ? "And" : "Or");
        return new EFCoreSpecification<T, TResult>(core, options);
    }
}

public static class EFCoreSpecification
{
    public static EFCoreSpecification<T, Composable> Create<T>() where T : class =>
        new(Specification.Create<T>(), new QueryOptions<T>());
}

/// <summary>Typed active Include result, still usable as an ordinary specification.</summary>
public interface IEFCoreIncludeSpecification<T, out TProperty, TState>
    where T : class
    where TState : SpecificationState
{
    EFCoreSpecification<T, TState> Specification { get; }
    string IncludePath { get; }
}

internal interface IActiveInclude<T> where T : class
{
    IIncludePlan<T> ActivePlan { get; }
}

public sealed class EFCoreIncludeSpecification<T, TProperty, TState> :
    EFCoreSpecification<T, TState>, IEFCoreIncludeSpecification<T, TProperty, TState>, IActiveInclude<T>
    where T : class
    where TState : SpecificationState
{
    internal EFCoreIncludeSpecification(
        Specification<T, TState> core, QueryOptions<T> options, IIncludePlan<T> plan)
        : base(core, options)
    {
        ActivePlan = plan;
        IncludePath = string.Join(".", plan.Segments.Select(segment => segment.Name));
    }

    IIncludePlan<T> IActiveInclude<T>.ActivePlan => ActivePlan;
    internal IIncludePlan<T> ActivePlan { get; }

    public EFCoreSpecification<T, TState> Specification => this;
    public string IncludePath { get; }
}

public static class EFCoreIncludeExtensions
{
    public static EFCoreIncludeSpecification<T, TProperty, TState> ThenInclude<T, TPrevious, TProperty, TState>(
        this IEFCoreIncludeSpecification<T, IEnumerable<TPrevious>, TState> specification,
        Expression<Func<TPrevious, TProperty>> expression)
        where T : class
        where TState : SpecificationState
    {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(expression);

        var active = specification.Specification.GetActivePlan();
        if (active is not ITypedIncludePlan<T, IEnumerable<TPrevious>> previous)
            throw new InvalidOperationException("Active Include is not a compatible collection navigation.");

        var plan = new IncludeChain<T, TProperty>(
            query => previous.ApplyTyped(query).ThenInclude(expression),
            active.Segments.Concat(IncludePath.Parse(expression)));
        return specification.Specification.AppendThenInclude<TProperty>(active, plan);
    }

    public static EFCoreIncludeSpecification<T, TProperty, TState> ThenInclude<T, TPrevious, TProperty, TState>(
        this IEFCoreIncludeSpecification<T, TPrevious, TState> specification,
        Expression<Func<TPrevious, TProperty>> expression)
        where T : class
        where TState : SpecificationState
    {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(expression);

        var active = specification.Specification.GetActivePlan();
        if (active is not ITypedIncludePlan<T, TPrevious> previous)
            throw new InvalidOperationException("Active Include is not a compatible reference navigation.");

        var plan = new IncludeChain<T, TProperty>(
            query => previous.ApplyTyped(query).ThenInclude(expression),
            active.Segments.Concat(IncludePath.Parse(expression)));
        return specification.Specification.AppendThenInclude<TProperty>(active, plan);
    }
}

public static class EFCoreCompositionExtensions
{
    public static EFCoreSpecification<T, Composable> And<T>(
        this EFCoreSpecification<T, Composable> left, EFCoreSpecification<T, Composable> right) where T : class =>
        left.Combine<Composable, Composable>(right, true);

    public static EFCoreSpecification<T, Configured> And<T>(
        this EFCoreSpecification<T, Composable> left, EFCoreSpecification<T, Configured> right) where T : class =>
        left.Combine<Configured, Configured>(right, true);

    public static EFCoreSpecification<T, Configured> And<T>(
        this EFCoreSpecification<T, Configured> left, EFCoreSpecification<T, Composable> right) where T : class =>
        left.Combine<Composable, Configured>(right, true);

    public static EFCoreSpecification<T, Configured> And<T>(
        this EFCoreSpecification<T, Configured> left, EFCoreSpecification<T, Configured> right) where T : class =>
        left.Combine<Configured, Configured>(right, true);

    public static EFCoreSpecification<T, Composable> Or<T>(
        this EFCoreSpecification<T, Composable> left, EFCoreSpecification<T, Composable> right) where T : class =>
        left.Combine<Composable, Composable>(right, false);

    public static EFCoreSpecification<T, Configured> Or<T>(
        this EFCoreSpecification<T, Composable> left, EFCoreSpecification<T, Configured> right) where T : class =>
        left.Combine<Configured, Configured>(right, false);

    public static EFCoreSpecification<T, Configured> Or<T>(
        this EFCoreSpecification<T, Configured> left, EFCoreSpecification<T, Composable> right) where T : class =>
        left.Combine<Composable, Configured>(right, false);

    public static EFCoreSpecification<T, Configured> Or<T>(
        this EFCoreSpecification<T, Configured> left, EFCoreSpecification<T, Configured> right) where T : class =>
        left.Combine<Configured, Configured>(right, false);
}
