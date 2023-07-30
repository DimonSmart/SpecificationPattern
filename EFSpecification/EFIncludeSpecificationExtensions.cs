using System.Linq.Expressions;

namespace DimonSmart.EFSpecification
{
   
    public static class EFIncludeSpecificationExtensions
    {
        public static IEFIncludeSpecification<T, TProperty> Include<T, TProperty>(
            this IEFSpecification<T> specification,
            Expression<Func<T, TProperty>> includeExpression) where T : class
        {
            // specification.ResetIncludeLevel();
            specification.AddInclude(GetPropertyName(includeExpression));
            return new EFIncludeSpecification<T, TProperty>(specification);
        }

        public static IEFIncludeSpecification<T, TProperty> ThenInclude<T, TPreviousProperty, TProperty>(
            this IEFIncludeSpecification<T, IEnumerable<TPreviousProperty>> specification,
            Expression<Func<TPreviousProperty, TProperty>> thenIncludeExpression) where T : class
        {
            specification.AddInclude(GetPropertyName(thenIncludeExpression));
            return new EFIncludeSpecification<T, TProperty>(specification);
        }

        public static string GetPropertyName<T, TProperty>(Expression<Func<T, TProperty>> includeExpression)
        {
            var body = includeExpression.Body as MemberExpression ?? throw new InvalidOperationException();
            var parts = new Stack<string>();
            while (body != null)
            {
                parts.Push(body.Member.Name);
                body = body.Expression as MemberExpression;
            }

            return string.Join(".", parts);
        }
    }
}




   // public static class EFIncludeSpecificationExtensions
   // {
        //public static IEFIncludeSpecification<T, TProperty> Include<T, TProperty>(
        //    this IEFSpecification<T> specification,
        //    Expression<Func<T, TProperty>> includeExpression) where T : class
        //{
        //    // specification.ResetIncludeLevel();
        //    specification.AddInclude(GetPropertyName(includeExpression));
        //    return new EFIncludeSpecification<T, TProperty>(specification);
        //}

        //public static IEFIncludeSpecification<T, TNextProperty> Include<T, TProperty, TNextProperty>(
        //    this IEFSpecification<T> specification,
        //    Expression<Func<T, IEnumerable<TProperty>>> includeExpression) where T : class
        //{
        //    // specification.ResetIncludeLevel();
        //    specification.AddInclude(GetPropertyName(includeExpression));
        //    return new EFIncludeSpecification<T, TNextProperty>(specification);
        //}


        //public static IEFIncludeSpecification<T, TProperty> ThenInclude<T, TPreviousProperty, TProperty>(
        //   this IEFIncludeSpecification<T, IEnumerable<TPreviousProperty>> specification,
        //   Expression<Func<TPreviousProperty, TProperty>> thenIncludeExpression) where T : class
        //{
        //    specification.AddInclude(GetPropertyName(thenIncludeExpression));
        //    return new EFIncludeSpecification<T, TProperty>(specification);
        //}

        //public static IEFIncludeSpecification<T, TProperty> ThenInclude<T, TPreviousProperty, TProperty>(
        //   this IEFIncludeSpecification<T, TPreviousProperty> specification,
        //   Expression<Func<TPreviousProperty, TProperty>> thenIncludeExpression) where T : class
        //{
        //    specification.AddInclude(GetPropertyName(thenIncludeExpression));
        //    return new EFIncludeSpecification<T, TProperty>(specification);
        //}




        //public static IEFIncludeSpecification<T, TProperty> ThenInclude<T, TPreviousProperty, TProperty>(
        //    this IEFIncludeSpecification<T, IEnumerable<TPreviousProperty>> specification,
        //    Expression<Func<TPreviousProperty, TProperty>> thenIncludeExpression) where T : class
        //{
        //    specification.AddInclude(GetPropertyName(thenIncludeExpression));
        //    return new EFIncludeSpecification<T, TProperty>(specification);
        //}


        //public static IEFIncludeSpecification<T, TPropertyOut> Include<T, TPropertyIn, TPropertyOut>(
        //    this IEFSpecification<T> specification,
        //    Expression<Func<T, TPropertyIn>> includeExpression) where T : class
        //{
        //    // specification.ResetIncludeLevel();
        //    specification.AddInclude(GetPropertyName(includeExpression));
        //    return new EFIncludeSpecification<T, TPropertyOut>(specification);
        //}



        //public static IEFIncludeSpecification<T, TProperty> ThenInclude<T, TProperty, TNextProperty>
        //    (this IEFIncludeSpecification<T, TProperty> spec,
        //    Expression<Func<TNextProperty, IEnumerable<TProperty>>> propertyExpression)
        //        where T : class
        //        where TProperty : class
        //{

        //    return null;
        //}


        //public static IEFIncludeSpecification<T, TProperty> ThenInclude<T, TPreviousProperty, TProperty>(
        //    this IEFSpecification<T> specification, Expression<Func<TPreviousProperty, IEnumerable<TProperty>>> thenIncludeExpression) where T : class
        //{
        //    // Implement your logic to handle ThenInclude here
        //    // return new EFIncludeSpecification<T, TNewProperty>();
        //    return null;
        //}

        //public static IEFIncludeSpecification<T, TProperty> ThenInclude<T, TPreviousProperty, TProperty>(
        //    this IEFIncludeSpecification<T, IEnumerable<TPreviousProperty>> specification,
        //    Expression<Func<TPreviousProperty, TProperty>> thenIncludeExpression) where T : class
        //{
        //    specification.AddInclude(GetPropertyName(thenIncludeExpression));
        //    return new EFIncludeSpecification<T, TProperty>(specification);
        //}



        //public static IEFIncludeSpecification<T, TNewProperty> ThenInclude<T, TPreviousProperty, TNewProperty>(
        //this IEFIncludeSpecification<T, IEnumerable<TPreviousProperty>> specification,
        //Expression<Func<TPreviousProperty, TNewProperty>> thenIncludeExpression) where T : class
        //{
        //    return null;

        //}




        //public static IEFIncludeSpecification<T, TProperty> ThenInclude<T, TPreviousProperty, TProperty>(
        //    this IEFIncludeSpecification<T, IEnumerable<TPreviousProperty>> specification,
        //    Expression<Func<TPreviousProperty, TProperty>> thenIncludeExpression) where T : class
        //{
        //    specification.AddInclude(GetPropertyName(thenIncludeExpression));
        //    return new EFIncludeSpecification<T, TProperty>(specification);
        //}

        //public static string GetPropertyName<T, TProperty>(Expression<Func<T, TProperty>> includeExpression)
        //{
        //    var body = includeExpression.Body as MemberExpression ?? throw new InvalidOperationException();
        //    var parts = new Stack<string>();
        //    while (body != null)
        //    {
        //        parts.Push(body.Member.Name);
        //        body = body.Expression as MemberExpression;
        //    }

        //    return string.Join(".", parts);
        //}
  //  }
//}