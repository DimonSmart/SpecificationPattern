using DimonSmart.Specification;
using DimonSmart.Specification.Tests.Common;

namespace DocumentationExamples;

public static class TypedOrderingExample
{
    public static IQueryable<Student> Query(IQueryable<Student> students)
    {
        var spec = Specification.Create<Student>()
            .OrderBy(s => s.Name)
            .ThenByDesc(s => s.Age)
            .ThenBy(s => s.Id);

        return students.BySpecification(spec);
    }
}
