namespace DimonSmart.Specification;

/// <summary>Read-only contract for evaluating immutable query specifications, including projections.</summary>
/// <remarks>The caller supplies the query root; no query is enumerated by these methods.</remarks>
public interface ISpecification<TSource, TResult> where TSource : class
{
    IReadOnlyList<QueryPipelineStep> PipelineSteps { get; }

    IQueryable<TResult> ApplyTo(IQueryable<TSource> query);

    IQueryable<TResult> ApplyCriteriaTo(IQueryable<TSource> query);
}

/// <summary>Specification whose result element type is the source entity type.</summary>
public interface ISpecification<T> : ISpecification<T, T> where T : class
{
    /// <summary>Tests the entity against specification criteria, excluding paging and ordering.</summary>
    bool IsSatisfiedBy(T candidate);
}
