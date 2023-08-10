using DimonSmart.Specification.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

[Collection("Database collection")]
public class DBIncludeTests : TestsBase
{
    private readonly SchoolContext _testDBContext;

    public DBIncludeTests(DatabaseFixture fixture)
    {
        Fixture = fixture;
        _testDBContext = fixture.TestDBContext;
    }

    public DatabaseFixture Fixture { get; }

    [Fact]
    public void IncludeTest()
    {
        // Arrange
        var specification = EFCoreSpecification<Student>
            .Create()
            .Where(s => s.Age < 21)
            .Include(s => s.School.MainBook.Author);

        // Act
        var under21 = _testDBContext.BySpecification(specification).IgnoreQueryFilters().ToList();

        // Assert
        under21
            .Should()
            .BeEquivalentTo(new List<Student> { Sofia20 }, options => options.Excluding(e => e.Books));
    }
}