namespace DimonSmart.Specification;

/// <summary>
/// Interface for building specifications for queries.
/// </summary>
/// <typeparam name="T">The type of entity being queried.</typeparam>
public interface ISpecification<T> : IBaseSpecification<T, ISpecification<T>> where T : class
{
}