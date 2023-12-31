using DimonSmart.Specification.Tests.Common;
using FluentAssertions;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

[Collection("Database collection")]
public class DBOrderTests : TestsBase
{
    private readonly SchoolContext _testDBContext;

    public DBOrderTests(DatabaseFixture fixture)
    {
        Fixture = fixture;
        _testDBContext = fixture.TestDBContext;
    }

    public DatabaseFixture Fixture { get; }

    [Fact]
    public void OneLevelOrderByTest()
    {
        // Arrange
        var specification = EFCoreSpecification<Student>
            .Create()
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
        var specification = EFCoreSpecification<Student>
            .Create()
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
        var specification = EFCoreSpecification<Student>
            .Create()
            .OrderBy(s => s.Name)
            .OrderByDesc(s => s.Age);

        // Act
        var ordered = _testDBContext.BySpecification(specification).ToList();

        // Assert
        ordered.Should().BeEquivalentTo(new List<Student> { Alex30, Alex22, Sofia20 });
    }
}