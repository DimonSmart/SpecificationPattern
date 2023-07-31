namespace DimonSmart.Specification;

public interface ISpecification<T> : IBaseSpecification<T, ISpecification<T>> where T : class
{
}