using DimonSmart.Specification.Tests.Common;
using FluentAssertions;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

[Collection("Database collection")]
public class DBLimitTests : TestsBase
{
    private readonly SchoolContext _testDBContext;

    public DBLimitTests(DatabaseFixture fixture)
    {
        Fixture = fixture;
        _testDBContext = fixture.TestDBContext;
    }

    public DatabaseFixture Fixture { get; }

    [Fact]
    public void TakeShouldLimitElementsQuantityTest()
    {
        // Arrange
        var specification = EFCoreSpecification<Student>
            .Create()
            .OrderBy(s => s.Name)
            .OrderBy(s => s.Age)
            .Take(1);

        // Act
        var limited = _testDBContext.BySpecification(specification).ToList();

        // Assert
        limited.Should().BeEquivalentTo(new List<Student> { Alex22 }, options => options.Excluding(e => e.Books));
    }

    [Fact]
    public void TakeAndSkipShouldWorkLikePagingTest()
    {
        // Arrange
        var specification = EFCoreSpecification<Student>
            .Create()
            .OrderBy(s => s.Name)
            .OrderBy(s => s.Age)
            .Take(2)
            .Skip(1);

        // Act
        var limited = _testDBContext.BySpecification(specification).ToList();

        // Assert
        limited.Should().BeEquivalentTo(new List<Student> { Alex30, Sofia20 }, options => options.Excluding(e => e.Books));
    }
}