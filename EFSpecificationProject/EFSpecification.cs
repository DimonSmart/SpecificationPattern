using SpecificationPattern;
using System.Linq.Expressions;

namespace EFSpecificationProject
{

    public class EFSpecification<T> : Specification<T>, IEFSpecification<T>
    {
        private List<string> Includes { get; } = new List<string>();
        public static new EFSpecification<T> Create() => new();
        public string CurrentIncludeLevel { get; private set; } = string.Empty;
        public bool IsAsNoTracking { get; private set; }
        public bool IsIgnoreAutoIncludes { get; private set; }

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
            CurrentIncludeLevel = include;
            if (CanBeExcluded(include))
            {
                return;
            }

            Includes.Add(include);
        }

        public bool CanBeExcluded(string newLine)
        {
            var newLineWithDot = newLine + ".";
            foreach (var existingLine in Includes)
            {
                var existingLineWithDot = existingLine + ".";

                if (existingLineWithDot.StartsWith(newLineWithDot))
                {
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyCollection<string> GetIncludes()
        {
            return Includes;
        }

        public IEFSpecification<T> IgnoreAutoIncludes()
        {

            IsIgnoreAutoIncludes = true;
            return this;
        }

        public IEFSpecification<T> AsNoTracking()
        {
            IsAsNoTracking = true;
            return this;
        }
    }
}