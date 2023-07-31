namespace DimonSmart.EFSpecification;

public interface IEFIncludeSpecification<T, out TProperty> : IEFSpecification<T> where T : class
{
}