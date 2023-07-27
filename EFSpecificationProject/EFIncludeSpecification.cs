using SpecificationPattern;
using System.Linq.Expressions;

namespace EFSpecificationProject
{
    public class EFIncludeSpecification<T, TProperty> : IEFIncludeSpecification<T, TProperty> where T : class
    {
        private readonly IEFSpecification<T> _parentSpecification;

        public EFIncludeSpecification(IEFSpecification<T> parentSpecification)
        {
            _parentSpecification = parentSpecification;
        }

        public Expression<Func<T, bool>>? WhereExpression => _parentSpecification.WhereExpression;

        public List<(bool direction, Expression<Func<T, object>> expr)> OrderExpressions => _parentSpecification.OrderExpressions;

        public int? TakeQ => _parentSpecification.TakeQ;

        public int? SkipQ => _parentSpecification.SkipQ;

        public bool IsAsNoTracking => _parentSpecification.IsAsNoTracking;

        public bool IsIgnoreAutoIncludes => _parentSpecification.IsIgnoreAutoIncludes;

        public void AddInclude(string include)
        {
            _parentSpecification.AddInclude(include);
        }

        public ISpecification<T> And(ISpecification<T> and)
        {
            return _parentSpecification.And(and);
        }

        public IReadOnlyCollection<string> GetIncludes()
        {
            return _parentSpecification.GetIncludes();
        }

        public IEFSpecification<T> AsNoTracking()
        {
            return _parentSpecification.AsNoTracking();
        }
        public IEFSpecification<T> IgnoreAutoIncludes()
        {
            return _parentSpecification.IgnoreAutoIncludes();
        }

        public ISpecification<T> Or(ISpecification<T> or)
        {
            return _parentSpecification.Or(or);
        }
        public EFIncludeSpecification<T, TProperty> ThenInclude(Expression<Func<TProperty, object>> includeExpression)
        {
            // TODO
            return this;
        }

        public IEFSpecification<T> Where(Expression<Func<T, bool>> expr)
        {
            return _parentSpecification.Where(expr);
        }

        ISpecification<T> ISpecification<T>.Where(Expression<Func<T, bool>> expr)
        {
            return _parentSpecification.Where(expr);
        }
    }
}