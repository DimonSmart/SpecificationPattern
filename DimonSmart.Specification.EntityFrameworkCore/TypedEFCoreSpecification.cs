
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace DimonSmart.Specification.EntityFrameworkCore;

/// <summary>
/// Immutable EF Core specification with Composable/Configured type-state.
/// Use EFCoreSpecification.Create{T}() to start a typed specification.
/// </summary>
public class EFCoreSpecification<T, TState>
    where T : class
    where TState : SpecificationState
{
    internal Specification<T, TState> Core { get; }
    private readonly EFCoreSpecificationData<T> _options;

    internal EFCoreSpecification(Specification<T, TState> core, EFCoreSpecificationData<T> options)
    {
        Core = core;
        _options = options;
    }

    public ISpecificationData<T> SpecificationData => Core.SpecificationData;
    public IEFCoreSpecificationData<T> EFCoreSpecificationData => CopyOptions(_options);

    internal static EFCoreSpecificationData<T> CopyOptions(IEFCoreSpecificationData<T> source)
    {
        var result = new EFCoreSpecificationData<T>
        {
            IsAsNoTracking = source.IsAsNoTracking,
            IsAsNoTrackingWithIdentityResolution = source.IsAsNoTrackingWithIdentityResolution,
            IsIgnoreAutoIncludes = source.IsIgnoreAutoIncludes,
            IsIgnoreQueryFilters = source.IsIgnoreQueryFilters,
            IsAsSingleQuery = source.IsAsSingleQuery,
            IsAsSplitQuery = source.IsAsSplitQuery,
            Tag = source.Tag
        };
        foreach (var path in source.Includes)
            result.AddInclude(path);
        return result;
    }

    public EFCoreSpecification<T, TState> Where(Expression<Func<T, bool>> predicate) =>
        new(Core.Where(predicate), CopyOptions(_options));

    public EFCoreSpecification<T, Configured> OrderBy(Expression<Func<T, object>> expression) =>
        new(Core.OrderBy(expression), CopyOptions(_options));

    public EFCoreSpecification<T, Configured> OrderByDesc(Expression<Func<T, object>> expression) =>
        new(Core.OrderByDesc(expression), CopyOptions(_options));

    public EFCoreSpecification<T, Configured> Take(int count) =>
        new(Core.Take(count), CopyOptions(_options));

    public EFCoreSpecification<T, Configured> Skip(int count) =>
        new(Core.Skip(count), CopyOptions(_options));

    public EFCoreSpecification<T, Configured> AsNoTracking()
    {
        var options = CopyOptions(_options);
        options.IsAsNoTracking = true;
        options.IsAsNoTrackingWithIdentityResolution = false;
        return new(CoreForConfigured(), options);
    }

    public EFCoreSpecification<T, Configured> AsNoTrackingWithIdentityResolution()
    {
        var options = CopyOptions(_options);
        options.IsAsNoTracking = false;
        options.IsAsNoTrackingWithIdentityResolution = true;
        return new(CoreForConfigured(), options);
    }

    public EFCoreSpecification<T, Configured> AsSplitQuery()
    {
        var options = CopyOptions(_options);
        options.IsAsSplitQuery = true;
        options.IsAsSingleQuery = false;
        return new(CoreForConfigured(), options);
    }

    public EFCoreSpecification<T, Configured> AsSingleQuery()
    {
        var options = CopyOptions(_options);
        options.IsAsSingleQuery = true;
        options.IsAsSplitQuery = false;
        return new(CoreForConfigured(), options);
    }

    public EFCoreSpecification<T, Configured> IgnoreAutoIncludes()
    {
        var options = CopyOptions(_options);
        options.IsIgnoreAutoIncludes = true;
        return new(CoreForConfigured(), options);
    }

    public EFCoreSpecification<T, Configured> IgnoreQueryFilters()
    {
        var options = CopyOptions(_options);
        options.IsIgnoreQueryFilters = true;
        return new(CoreForConfigured(), options);
    }

    public EFCoreSpecification<T, Configured> TagWith(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        var options = CopyOptions(_options);
        options.Tag = string.IsNullOrEmpty(options.Tag)
            ? tag
            : options.Tag + Environment.NewLine + tag;
        return new(CoreForConfigured(), options);
    }

    public EFCoreSpecification<T, Configured> TagWithCallSite(
        string tag = "",
        [CallerMemberName] string callerMemberName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        var callSite = callerMemberName + "() -> " + callerFilePath + ":" + callerLineNumber;
        return TagWith(string.IsNullOrWhiteSpace(tag) ? callSite : tag + Environment.NewLine + callSite);
    }

    private Specification<T, Configured> CoreForConfigured() =>
        new(Specification<T, TState>.Copy(Core.Data));

    public EFCoreTypedIncludeSpecification<T, TProperty, TState> Include<TProperty>(
        Expression<Func<T, TProperty>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var path = EFCoreIncludeSpecificationExtensions.GetPropertyName(expression);
        return AppendInclude<TProperty>(path);
    }

    internal EFCoreTypedIncludeSpecification<T, TProperty, TState> AppendInclude<TProperty>(string path)
    {
        var options = CopyOptions(_options);
        options.AddInclude(path);
        return new EFCoreTypedIncludeSpecification<T, TProperty, TState>(Core, options, path);
    }

    internal EFCoreSpecification<T, TResult> Combine<TOther, TResult>(
        EFCoreSpecification<T, TOther> other, bool and)
        where TOther : SpecificationState
        where TResult : SpecificationState
    {
        ArgumentNullException.ThrowIfNull(other);
        var left = _options;
        var right = other._options;

        if ((left.IsAsNoTracking && right.IsAsNoTrackingWithIdentityResolution) ||
            (left.IsAsNoTrackingWithIdentityResolution && right.IsAsNoTracking))
            throw new InvalidOperationException("Cannot combine specifications: conflicting tracking modes.");
        if ((left.IsAsSplitQuery && right.IsAsSingleQuery) ||
            (left.IsAsSingleQuery && right.IsAsSplitQuery))
            throw new InvalidOperationException("Cannot combine specifications: conflicting query splitting modes.");

        // Core.Combine rejects duplicate sorting, Skip and Take before producing a result.
        var core = Core.Combine<TOther, TResult>(other.Core, and);
        var options = CopyOptions(left);
        foreach (var include in right.Includes)
            options.AddInclude(include);
        options.IsAsNoTracking |= right.IsAsNoTracking;
        options.IsAsNoTrackingWithIdentityResolution |= right.IsAsNoTrackingWithIdentityResolution;
        options.IsAsSingleQuery |= right.IsAsSingleQuery;
        options.IsAsSplitQuery |= right.IsAsSplitQuery;
        options.IsIgnoreAutoIncludes |= right.IsIgnoreAutoIncludes;
        options.IsIgnoreQueryFilters |= right.IsIgnoreQueryFilters;
        if (!string.IsNullOrEmpty(right.Tag))
            options.Tag = string.IsNullOrEmpty(options.Tag)
                ? right.Tag : options.Tag + Environment.NewLine + right.Tag;
        return new EFCoreSpecification<T, TResult>(core, options);
    }
}

public static class EFCoreSpecification
{
    public static EFCoreSpecification<T, Composable> Create<T>() where T : class =>
        new(Specification.Create<T>(), new EFCoreSpecificationData<T>());
}

/// <summary>A typed include result which is also a specification for further composition.</summary>
public interface IEFCoreTypedIncludeSpecification<T, out TProperty, TState>
    where T : class
    where TState : SpecificationState
{
    EFCoreSpecification<T, TState> Specification { get; }
    string IncludePath { get; }
}

public sealed class EFCoreTypedIncludeSpecification<T, TProperty, TState> :
    EFCoreSpecification<T, TState>, IEFCoreTypedIncludeSpecification<T, TProperty, TState>
    where T : class
    where TState : SpecificationState
{
    internal EFCoreTypedIncludeSpecification(
        Specification<T, TState> core, EFCoreSpecificationData<T> options, string includePath)
        : base(core, options) => IncludePath = includePath;

    public EFCoreSpecification<T, TState> Specification => this;
    public string IncludePath { get; }
}

public static class EFCoreTypedIncludeExtensions
{
    public static EFCoreTypedIncludeSpecification<T, TProperty, TState> ThenInclude<T, TPrevious, TProperty, TState>(
        this IEFCoreTypedIncludeSpecification<T, IEnumerable<TPrevious>, TState> specification,
        Expression<Func<TPrevious, TProperty>> expression)
        where T : class
        where TState : SpecificationState =>
        specification.Specification.AppendInclude<TProperty>(
            specification.IncludePath + "." + EFCoreIncludeSpecificationExtensions.GetPropertyName(expression));

    public static EFCoreTypedIncludeSpecification<T, TProperty, TState> ThenInclude<T, TPrevious, TProperty, TState>(
        this IEFCoreTypedIncludeSpecification<T, TPrevious, TState> specification,
        Expression<Func<TPrevious, TProperty>> expression)
        where T : class
        where TState : SpecificationState =>
        specification.Specification.AppendInclude<TProperty>(
            specification.IncludePath + "." + EFCoreIncludeSpecificationExtensions.GetPropertyName(expression));
}

public static class EFCoreTypedCompositionExtensions
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
