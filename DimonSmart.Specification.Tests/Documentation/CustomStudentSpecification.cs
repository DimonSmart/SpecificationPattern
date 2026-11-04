using DimonSmart.Specification;

namespace DocumentationExamples;

public sealed record School(string Name);
public sealed record Student(int Age, School School);

public sealed class CustomStudentSpecification : Specification<Student, Composable>
{
    public CustomStudentSpecification(int minAge, string schoolName)
        : base(Specification.Create<Student>()
            .Where(s => s.Age >= minAge && s.School.Name == schoolName))
    {
    }
}
