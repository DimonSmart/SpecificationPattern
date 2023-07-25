using SpecificationPattern;
using System.Linq.Expressions;

namespace EFSpecificationProject
{

    public class EFSpecification<T> : Specification<T>, IEFSpecification<T>
    {
        public static new EFSpecification<T> Create() => new();
        public List<string> Includes { get; } = new List<string>();

        public string CurrentIncludeLevel { get; private set; } = string.Empty;

        public bool AsNoTracking { get; private set; }

        public override IEFSpecification<T> Where(Expression<Func<T, bool>> expr)
        {
            base.Where(expr);
            return this;
        }

        public EFSpecification<T> And(EFSpecification<T> and)
        {
            Includes.AddRange(and.Includes);
            _ = And(and.WhereExpression);
            return this;
        }

        public EFSpecification<T> Or(EFSpecification<T> and)
        {
            Includes.AddRange(and.Includes);
            _ = Or(and.WhereExpression);
            return this;
        }

        public void AddInclude(string include)
        {
            include = string.IsNullOrEmpty(CurrentIncludeLevel) ? include : $"{CurrentIncludeLevel}.{include}";
            Includes.Add(include);
            CurrentIncludeLevel = include;
        }
    }
}