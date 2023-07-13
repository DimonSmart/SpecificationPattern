using SpecificationPattern;
using System.Linq.Expressions;

namespace EFSpecificationProject
{
    public class EFSpecification<T> : Specification<T>
    {
        public EFSpecification(Expression<Func<T, bool>> expr) : base(expr)
        {
        }
        public List<Expression<Func<T, object>>> IncludeExpressions { get; } =
            new List<Expression<Func<T, object>>>();

        public bool AsNoTracking { get; private set; }

        public EFSpecification<T> Include(Expression<Func<T, object>> includeExpression)
        {
            IncludeExpressions.Add(includeExpression);
            return this;
        }

        public EFSpecification<T> And(EFSpecification<T> and)
        {
            IncludeExpressions.AddRange(and.IncludeExpressions);
            _ = And(and.WhereExpression);
            return this;
        }

        public EFSpecification<T> Or(EFSpecification<T> and)
        {
            IncludeExpressions.AddRange(and.IncludeExpressions);
            _ = Or(and.WhereExpression);
            return this;
        }
    }
}