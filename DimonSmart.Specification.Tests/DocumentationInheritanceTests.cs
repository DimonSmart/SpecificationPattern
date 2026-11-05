using DocumentationExamples;
using FluentAssertions;

namespace DimonSmart.Specification.Tests;

public class DocumentationInheritanceTests
{
    [Fact]
    public void ReadmeInheritanceExampleCompilesAndFilters()
    {
        var students = new[]
        {
            new DocumentationExamples.Student(17, new DocumentationExamples.School("High School")),
            new DocumentationExamples.Student(19, new DocumentationExamples.School("High School")),
            new DocumentationExamples.Student(22, new DocumentationExamples.School("University"))
        }.AsQueryable();

        var specification = new CustomStudentSpecification(18, "High School");

        students.BySpecification(specification).Select(s => s.Age).ToArray()
            .Should().Equal(19);
    }

    [Fact]
    public void DerivedSpecificationRemainsComposableAndImmutable()
    {
        Specification<DocumentationExamples.Student> specification =
            new CustomStudentSpecification(18, "High School");

        var ordered = specification.OrderBy(s => s.Age);

        specification.SpecificationData.OrderExpressions.Should().BeEmpty();
        ordered.SpecificationData.OrderExpressions.Should().HaveCount(1);
    }
}
