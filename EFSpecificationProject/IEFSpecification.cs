using SpecificationPattern;
using System.Linq.Expressions;

namespace EFSpecificationProject
{
    public interface IEFSpecification<T> : ISpecification<T>
    {
        void AddInclude(string include);

        public IReadOnlyCollection<string> GetIncludes();

        new IEFSpecification<T> Where(Expression<Func<T, bool>> expr);
    }
}