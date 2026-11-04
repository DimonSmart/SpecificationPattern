namespace DimonSmart.Specification.EntityFrameworkCore;

/// <summary>
/// Terminal EF Core specification retaining immutable query options and the
/// source LINQ projection. No specification builder operations follow Select.
/// </summary>
public sealed class EFCoreProjectedSpecification<TEntity, TResult>
    where TEntity : class
{
    internal ProjectedSpecification<TEntity, TResult> Projection { get; }
    internal QueryOptions<TEntity> Options { get; }

    internal EFCoreProjectedSpecification(
        ProjectedSpecification<TEntity, TResult> projection,
        QueryOptions<TEntity> options)
    {
        Projection = projection;
        Options = options;
    }
}
