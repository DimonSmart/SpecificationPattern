using DimonSmart.Specification.EntityFrameworkCore;
using DimonSmart.Specification.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace DocumentationExamples;

public static class TrackingExample
{
    public static IQueryable<Student> Query(DbContext dbContext)
    {
        var specification = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age >= 18)
            .AsTracking();

        return dbContext.BySpecification(specification);
    }
}
