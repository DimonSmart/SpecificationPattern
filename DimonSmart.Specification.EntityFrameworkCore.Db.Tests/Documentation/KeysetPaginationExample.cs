using DimonSmart.Specification.EntityFrameworkCore;
using DimonSmart.Specification.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace DocumentationExamples;

public static class KeysetPaginationExample
{
    public static IQueryable<Student> NextPage(
        DbContext dbContext, int lastAge, int lastId) =>
        dbContext.BySpecification(
            EFCoreSpecification.Create<Student>()
                .Where(s => s.Age >= 18)
                .OrderByDesc(s => s.Age)
                .OrderBy(s => s.Id)
                .SeekAfter((lastAge, lastId))
                .Take(20));
}
