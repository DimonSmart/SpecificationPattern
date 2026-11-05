using DimonSmart.Specification;
using StudentEntity = DimonSmart.Specification.Tests.Common.Student;

namespace DocumentationExamples;

public sealed record StudentAgeView(int Id, int Age);

public static class EnumerableBySpecificationExample
{
    public static IEnumerable<StudentEntity> SelectAdults(IEnumerable<StudentEntity> students)
    {
        var specification = Specification.Create<StudentEntity>()
            .Where(s => s.Age >= 18)
            .OrderBy(s => s.Age)
            .Take(10);

        return students.BySpecification(specification);
    }

    public static IEnumerable<StudentAgeView> SelectAdultAges(IEnumerable<StudentEntity> students)
    {
        var specification = Specification.Create<StudentEntity>()
            .Where(s => s.Age >= 18)
            .OrderBy(s => s.Id)
            .Select(s => new StudentAgeView(s.Id, s.Age));

        return students.BySpecification(specification);
    }
}
