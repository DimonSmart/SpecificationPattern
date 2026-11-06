using Microsoft.EntityFrameworkCore;

namespace DimonSmart.Specification.EntityFrameworkCore;

public static class EFCoreSpecificationExtension
{
    /// <summary>Applies an EF Core specification to the context's entity set.</summary>
    public static IQueryable<T> BySpecification<T>(
        this DbContext context, EFCoreSpecification<T> specification)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(specification);

        return context.Set<T>().BySpecification(specification);
    }

    /// <summary>
    /// Applies only Where criteria and global query-filter settings to the context's entity set.
    /// </summary>
    public static IQueryable<T> BySpecificationCriteria<T>(
        this DbContext context, EFCoreSpecification<T> specification)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(specification);

        return context.Set<T>().BySpecificationCriteria(specification);
    }

    /// <summary>Applies an EF Core projected specification to the context's entity set.</summary>
    public static IQueryable<TResult> BySpecification<TEntity, TResult>(
        this DbContext context, EFCoreProjectedSpecification<TEntity, TResult> specification)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(specification);

        return context.Set<TEntity>().BySpecification(specification);
    }
    /// <summary>Applies only criteria and type-changing projections, without specification paging.</summary>
    public static IQueryable<TCurrent> BySpecificationCriteria<TSource, TCurrent>(
        this DbContext context, EFCoreProjectedSpecification<TSource, TCurrent> specification)
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(specification);
        return context.Set<TSource>().BySpecificationCriteria(specification);
    }

    public static IQueryable<T> BySpecification<T>(
        this DbContext context, IEFCoreSpecification<T> specification) where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(specification);
        return specification.ApplyTo(context.Set<T>());
    }

    public static IQueryable<T> BySpecificationCriteria<T>(
        this DbContext context, IEFCoreSpecification<T> specification) where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(specification);
        return specification.ApplyCriteriaTo(context.Set<T>());
    }

    public static IQueryable<TResult> BySpecification<TSource, TResult>(
        this DbContext context, IEFCoreSpecification<TSource, TResult> specification)
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(specification);
        return specification.ApplyTo(context.Set<TSource>());
    }

    public static IQueryable<TResult> BySpecificationCriteria<TSource, TResult>(
        this DbContext context, IEFCoreSpecification<TSource, TResult> specification)
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(specification);
        return specification.ApplyCriteriaTo(context.Set<TSource>());
    }

}
