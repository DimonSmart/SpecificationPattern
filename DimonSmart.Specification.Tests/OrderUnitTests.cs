using DimonSmart.Specification.Tests.Common;
using FluentAssertions;

namespace DimonSmart.Specification.Tests;

public class OrderUnitTests : TestsBase
{
    [Fact]
    public void OneLevelOrderByTest()
    {
        // Arrange
        var specification = Specification.Create<Student>()
            .OrderBy(s => s.Age);

        // Act
        var ordered = Students.BySpecification(specification).ToList();

        // Assert
        ordered.Should().BeInAscendingOrder(i => i.Age);
    }


    [Fact]
    public void OneLevelOrderByDescTest()
    {
        // Arrange
        var specification = Specification.Create<Student>()
            .OrderByDesc(s => s.Age);

        // Act
        var ordered = Students.BySpecification(specification).ToList();

        // Assert
        ordered.Should().BeInDescendingOrder(i => i.Age);
    }

    [Fact]
    public void TwoLevelOrderByTest()
    {
        // Arrange
        var specification = Specification.Create<Student>()
            .OrderBy(s => s.Name)
            .OrderByDesc(s => s.Age);

        // Act
        var ordered = Students.BySpecification(specification).ToList();

        // Assert
        ordered.Should().BeEquivalentTo(new List<Student> { Alex30, Alex22, Sofia20 });
    }

    [Fact]
    public void ThreeLevelOrderByPreservesIntermediateSortTest()
    {
        var students = new[]
        {
            new Student { Id = 4, Age = 20, Name = "B" },
            new Student { Id = 1, Age = 20, Name = "A" },
            new Student { Id = 2, Age = 20, Name = "A" },
            new Student { Id = 3, Age = 20, Name = "A" },
            new Student { Id = 5, Age = 19, Name = "Z" }
        }.AsQueryable();

        var specification = Specification.Create<Student>()
            .OrderBy(s => s.Age)
            .OrderBy(s => s.Name)
            .OrderByDesc(s => s.Id);

        var orderedIds = students.BySpecification(specification)
            .Select(s => s.Id)
            .ToList();

        orderedIds.Should().Equal(5, 3, 2, 1, 4);
    }
}