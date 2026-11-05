using DimonSmart.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudentEntity = DimonSmart.Specification.Tests.Common.Student;

namespace DocumentationExamples;

public static class QueryableEFCoreExample
{
    public static IQueryable<StudentEntity> Query(DbContext context, int minimumAge)
    {
        var specification = EFCoreSpecification.Create<StudentEntity>()
            .Where(s => s.Name.StartsWith("A"))
            .Include(s => s.School)
            .AsNoTracking()
            .OrderBy(s => s.Id)
            .Take(20);

        return context.Set<StudentEntity>()
            .Where(s => s.Age >= minimumAge)
            .BySpecification(specification);
    }

    public static IQueryable<string> Names(DbContext context, int minimumAge)
    {
        var specification = EFCoreSpecification.Create<StudentEntity>()
            .OrderBy(s => s.Id)
            .AsNoTracking()
            .Select(s => s.Name);

        return context.Set<StudentEntity>()
            .Where(s => s.Age >= minimumAge)
            .BySpecification(specification);
    }
}
