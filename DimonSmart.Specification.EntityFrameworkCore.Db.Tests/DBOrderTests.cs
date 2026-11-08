using DimonSmart.Specification.Tests.Common;
using FluentAssertions;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

[Collection("Database collection")]
public class DBOrderTests : TestsBase
{
    private readonly SchoolContext _testDBContext;

    public DBOrderTests(DatabaseFixture fixture) => _testDBContext = fixture.TestDBContext;

    [Fact]
    public void OneLevelOrderByTest()
    {
        // Arrange
        var specification = EFCoreSpecification.Create<Student>()
            .OrderBy(s => s.Age);

        // Act
        var ordered = _testDBContext.BySpecification(specification).ToList();

        // Assert
        ordered.Should().BeInAscendingOrder(i => i.Age);
    }

    [Fact]
    public void OneLevelOrderByDescTest()
    {
        // Arrange
        var specification = EFCoreSpecification.Create<Student>()
            .OrderByDesc(s => s.Age);

        // Act
        var ordered = _testDBContext.BySpecification(specification).ToList();

        // Assert
        ordered.Should().BeInDescendingOrder(i => i.Age);
    }

    [Fact]
    public void TwoLevelOrderByTest()
    {
        // Arrange
        var specification = EFCoreSpecification.Create<Student>()
            .OrderBy(s => s.Name)
            .ThenByDesc(s => s.Age);

        // Act
        var ordered = _testDBContext.BySpecification(specification).ToList();

        // Assert
        ordered.Should().Equal(Alex30, Alex22, Sofia20);
    }

    [Fact]
    public void ThreeLevelOrderByPreservesIntermediateSortTest()
    {
        var specification = EFCoreSpecification.Create<Student>()
            .OrderBy(s => s.School.Name)
            .ThenBy(s => s.Name)
            .ThenBy(s => s.Age);

        var orderedAges = _testDBContext.BySpecification(specification)
            .Select(s => s.Age)
            .ToList();

        orderedAges.Should().Equal(22, 30, 20);
    }
}