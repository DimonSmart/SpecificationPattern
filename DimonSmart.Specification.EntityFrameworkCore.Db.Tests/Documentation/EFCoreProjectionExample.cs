using DimonSmart.Specification.EntityFrameworkCore;
using DimonSmart.Specification.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace DocumentationExamples;

public sealed record StudentDto(int Id, string Name, string SchoolName);

public static class EFCoreProjectionExample
{
    public static async Task<List<StudentDto>> QueryAsync(DbContext dbContext)
    {
        var adults = EFCoreSpecification.Create<Student>().Where(s => s.Age >= 18);
        var page = EFCoreSpecification.Create<Student>()
            .OrderBy(s => s.Name).Skip(10).Take(20).AsNoTracking();

        var projection = adults.And(page)
            .Select(s => new StudentDto(s.Id, s.Name, s.School.Name));

        return await dbContext.BySpecification(projection).ToListAsync();
    }
}
