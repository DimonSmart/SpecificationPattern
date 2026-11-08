using DimonSmart.Specification.Tests.Common;
using FluentAssertions;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

[Collection("Database collection")]
public class DBSplitQueryTests : TestsBase
{
    private readonly EFCoreSpecification<Student> _splitTestSpecification = EFCoreSpecification.Create<Student>()
        .Include(s => s.Books)
        .ThenInclude(b => b.Author)
        .OrderBy(s => s.Name)
        .ThenBy(s => s.Age);

    private readonly SchoolContext _testDBContext;

    public DBSplitQueryTests(DatabaseFixture fixture) => _testDBContext = fixture.TestDBContext;

    [Fact]
    public void QueryShouldBeSplit()
    {
        // Arrange
        _testDBContext.DbCommandsLog.Clear();

        // Act
        var results = _testDBContext.BySpecification(_splitTestSpecification.AsSplitQuery()).ToList();

        // Assert
        results.Select(s => s.Age).Should().Equal(22, 30, 20);
        _testDBContext.DbCommandsLog.Count.Should().Be(2);
    }

    [Fact]
    public void QueryShouldNotBeSplit()
    {
        // Arrange
        _testDBContext.DbCommandsLog.Clear();

        // Act
        var results = _testDBContext.BySpecification(_splitTestSpecification.AsSingleQuery()).ToList();

        // Assert
        results.Select(s => s.Age).Should().Equal(22, 30, 20);
        _testDBContext.DbCommandsLog.Count.Should().Be(1);
    }
}