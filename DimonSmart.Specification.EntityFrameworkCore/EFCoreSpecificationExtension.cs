using Microsoft.EntityFrameworkCore;

namespace DimonSmart.Specification.EntityFrameworkCore;

public static class EFCoreSpecificationExtension
{
    /// <summary>Applies an EF Core specification to the context's entity set.</summary>
    public static IQueryable<T> BySpecification<T, TState>(
        this DbContext context, EFCoreSpecification<T, TState> specification)
        where T : class
        where TState : SpecificationState
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(specification);

        return context.Set<T>().BySpecification(specification);
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
}
