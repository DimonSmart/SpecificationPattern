using DimonSmart.Specification.EntityFrameworkCore;
using DimonSmart.Specification.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace DocumentationExamples;

public static class ConditionalWhereExample
{
    public static IQueryable<Student> Query(DbContext dbContext, string? name, int? maxAge)
    {
        var specification = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age >= 18)
            .Where(s => s.Name == name, !string.IsNullOrWhiteSpace(name))
            .Where(s => s.Age <= maxAge, maxAge.HasValue)
            .OrderBy(s => s.Id)
            .Take(20);

        return dbContext.BySpecification(specification);
    }
}
