using System.Linq.Expressions;

namespace DimonSmart.Specification;

/// <summary>
/// Terminal, immutable LINQ specification: filtering, ordering and paging
/// operate on TEntity before the projection to TResult.
/// </summary>
public sealed class ProjectedSpecification<TEntity, TResult>
    where TEntity : class
{
    internal ISpecificationData<TEntity> SourceData { get; }
    internal Expression<Func<TEntity, TResult>> Selector { get; }

    internal ProjectedSpecification(
        ISpecificationData<TEntity> sourceData,
        Expression<Func<TEntity, TResult>> selector)
    {
        SourceData = Specification<TEntity, Composable>.Copy(sourceData);
        Selector = selector;
    }
}
