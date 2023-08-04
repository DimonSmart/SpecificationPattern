namespace DimonSmart.Specification.EntityFrameworkCore;

public interface IEFIncludeSpecification<T, out TProperty> : IEFSpecification<T> where T : class
{
}