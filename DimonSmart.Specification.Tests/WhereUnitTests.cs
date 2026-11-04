using DimonSmart.Specification.Tests.Common;
using FluentAssertions;

namespace DimonSmart.Specification.Tests;

public class WhereUnitTests : TestsBase
{
    [Fact]
    public void SimpleWhereConditionTest()
    {
        // Arrange
        var specification = Specification.Create<Student>().Where(s => s.Age < 21);

        // Act
        var under21 = Students.BySpecification(specification).ToList();

        // Assert
        under21.Should().BeEquivalentTo(new List<Student> { Sofia20 });
    }

    [Fact]
    public void WhereWithOrClauseTest()
    {
        // Arrange
        var specification1 = Specification.Create<Student>()
            .Where(s => s.Age < 21);

        var specification2 = Specification.Create<Student>()
            .Where(s => s.Age == 30);

        var specification = specification1.Or(specification2);

        // Act
        var under21 = Students.BySpecification(specification).ToList();

        // Assert
        under21.Should().BeEquivalentTo(new List<Student> { Sofia20, Alex30 });
    }

    [Fact]
    public void WhereWithAndClauseTest()
    {
        // Arrange
        var specification1 = Specification.Create<Student>()
            .Where(s => s.Age < 30);

        var specification2 = Specification.Create<Student>()
            .Where(s => s.Age > 20);

        var specification = specification1.And(specification2);

        // Act
        var under30AndAbove20 = Students.BySpecification(specification).ToList();

        // Assert
        under30AndAbove20.Should().BeEquivalentTo(new List<Student> { Alex22 });
    }

    [Fact]
    public void AndPreservesNestedLambdaParameter()
    {
        var olderStudents = Specification.Create<Student>()
            .Where(student => student.Age > 20);
        var hasGatsby = Specification.Create<Student>()
            .Where(student => student.Books.Any(book => book.Name == "The Great Gatsby"));

        var result = olderStudents.And(hasGatsby);

        Students.BySpecification(result).ToList()
            .Should().BeEquivalentTo(new[] { Alex22 });
    }

    [Fact]
    public void OrPreservesOuterReferencesInsideNestedLambda()
    {
        var peers = Students.ToList();
        var hasYoungerPeer = Specification.Create<Student>()
            .Where(student => peers.Any(peer => peer.Age < student.Age));
        var isSofia = Specification.Create<Student>()
            .Where(student => student.Name == "Sofia");

        var result = isSofia.Or(hasYoungerPeer);

        Students.BySpecification(result).ToList()
            .Should().BeEquivalentTo(new[] { Sofia20, Alex22, Alex30 });
    }

    [Fact]
    public void RepeatedWherePreservesNestedLambdaParameters()
    {
        var result = Specification.Create<Student>()
            .Where(student => student.Age > 20)
            .Where(student => student.Books.Any(book => book.Name == "The Great Gatsby"));

        Students.BySpecification(result).ToList()
            .Should().BeEquivalentTo(new[] { Alex22 });
    }
}
