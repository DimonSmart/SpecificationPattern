namespace DimonSmart.Specification.EntityFrameworkCore;

/// <summary>Execution contract for EF Core specifications without exposing mutable query state.</summary>
public interface IEFCoreSpecification<T> : ISpecification<T> where T : class
{
    IEFCoreSpecificationData<T> EFCoreSpecificationData { get; }
}

/// <summary>Execution contract for projected EF Core specifications.</summary>
public interface IEFCoreSpecification<TSource, TResult> : ISpecification<TSource, TResult>
    where TSource : class
{
    IEFCoreSpecificationData<TSource> EFCoreSpecificationData { get; }
}
