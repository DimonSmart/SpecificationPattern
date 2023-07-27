using SpecificationPattern;
using System.Linq.Expressions;

namespace EFSpecificationProject
{
    public interface IEFSpecification<T> : ISpecification<T>
    {
        void AddInclude(string include);
        public IReadOnlyCollection<string> GetIncludes();

        bool IsAsNoTracking { get; }
        IEFSpecification<T> AsNoTracking();

        bool IsIgnoreAutoIncludes { get; }
        IEFSpecification<T> IgnoreAutoIncludes();

        new IEFSpecification<T> Where(Expression<Func<T, bool>> expr);
    }
}