namespace DimonSmart.Specification.EntityFrameworkCore;

public interface IEFCoreIncludeSpecification<T, out TProperty> : IEFCoreSpecification<T> where T : class
{
}