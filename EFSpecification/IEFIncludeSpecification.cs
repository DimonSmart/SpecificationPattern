using DimonSmart.EFSpecification;
using System.Linq.Expressions;

namespace DimonSmart.EFSpecification
{
    public interface IEFIncludeSpecification<T, out TProperty> : IEFSpecification<T> where T : class
    {
        //IEFIncludeSpecification<T, TNextProperty> ThenInclude<TNextProperty, TProperty>(
        //    IEFIncludeSpecification<T, IEnumerable<TNextProperty>> specification,
        //    Expression<Func<TPreviousProperty, TProperty>> thenIncludeExpression) where T : class
        //{
        //    specification.AddInclude(GetPropertyName(thenIncludeExpression));
        //    return new EFIncludeSpecification<T, TProperty>(specification);
        //}


        // IEFIncludeSpecification<T, TNextProperty> Include<TNextProperty>(Expression<Func<TProperty, TNextProperty>> includeExpression);
        // IEFIncludeSpecification<T, TNextProperty> Include<TNextProperty>(Expression<Func<TProperty, IEnumerable<TNextProperty>>> includeExpression);


        //   IEFIncludeSpecification<T, TProperty> ThenInclude(Expression<Func<TProperty, object>> includeExpression);
        //   IEFIncludeSpecification<T, TProperty> ThenInclude<TNextProperty>(Func<TNextProperty, IEnumerable<TProperty>> value);
    }
}