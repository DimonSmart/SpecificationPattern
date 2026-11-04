using DimonSmart.Specification.EntityFrameworkCore;
using DimonSmart.Specification.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace DocumentationExamples;

public static class FilteredIncludeExample
{
    public static IQueryable<Student> Query(DbContext dbContext)
    {
        var books = EFCoreSpecification.Create<Student>()
            .Include(s => s.Books
                .Where(b => b.Name.StartsWith("A"))
                .OrderBy(b => b.Name)
                .Take(5))
            .ThenInclude(b => b.Author);

        var school = EFCoreSpecification.Create<Student>()
            .Include(s => s.School);

        return dbContext.BySpecification(books.And(school).AsNoTracking());
    }
}
