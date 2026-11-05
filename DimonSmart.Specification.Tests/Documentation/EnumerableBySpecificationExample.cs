using DimonSmart.Specification;
using DimonSmart.Specification.Tests.Common;

namespace DocumentationExamples;

public sealed record StudentAgeView(int Id, int Age);

public static class EnumerableBySpecificationExample
{
    public static IEnumerable<Student> SelectAdults(IEnumerable<Student> students)
    {
        var specification = Specification.Create<Student>()
            .Where(s => s.Age >= 18)
            .OrderBy(s => s.Age)
            .Take(10);

        return students.BySpecification(specification);
    }

    public static IEnumerable<StudentAgeView> SelectAdultAges(IEnumerable<Student> students)
    {
        var specification = Specification.Create<Student>()
            .Where(s => s.Age >= 18)
            .OrderBy(s => s.Id)
            .Select(s => new StudentAgeView(s.Id, s.Age));

        return students.BySpecification(specification);
    }
}
