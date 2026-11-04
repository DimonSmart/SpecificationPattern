using DimonSmart.Specification;
using StudentEntity = DimonSmart.Specification.Tests.Common.Student;

namespace DocumentationExamples;

public static class TypedOrderingExample
{
    public static IQueryable<StudentEntity> Query(IQueryable<StudentEntity> students)
    {
        var spec = Specification.Create<StudentEntity>()
            .OrderBy(s => s.Name)
            .ThenByDesc(s => s.Age)
            .ThenBy(s => s.Id);

        return students.BySpecification(spec);
    }
}
