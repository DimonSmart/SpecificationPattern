using DimonSmart.Specification;
using StudentEntity = DimonSmart.Specification.Tests.Common.Student;

namespace DocumentationExamples;

public sealed record StudentSummary(int Id, string Name);

public static class ProjectionExample
{
    public static IQueryable<StudentSummary> Query(IQueryable<StudentEntity> students)
    {
        var adults = Specification.Create<StudentEntity>().Where(s => s.Age >= 18);
        var page = Specification.Create<StudentEntity>()
            .OrderBy(s => s.Name).Skip(10).Take(20);

        var projection = adults.And(page)
            .Select(s => new StudentSummary(s.Id, s.Name));

        return students.BySpecification(projection);
    }
}
