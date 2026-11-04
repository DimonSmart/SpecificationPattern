using DimonSmart.Specification;
using DimonSmart.Specification.Tests.Common;

namespace DocumentationExamples;

public sealed record StudentSummary(int Id, string Name);

public static class ProjectionExample
{
    public static IQueryable<StudentSummary> Query(IQueryable<Student> students)
    {
        var adults = Specification.Create<Student>().Where(s => s.Age >= 18);
        var page = Specification.Create<Student>()
            .OrderBy(s => s.Name).Skip(10).Take(20);

        var projection = adults.And(page)
            .Select(s => new StudentSummary(s.Id, s.Name));

        return students.BySpecification(projection);
    }
}
