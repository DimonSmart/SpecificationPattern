using DimonSmart.Specification.Tests.Common;
using FluentAssertions;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

[Collection("Database collection")]
public class DBSplitQueryTests : TestsBase
{
    private readonly IEFCoreSpecification<Student> _splitTestSpecification = EFCoreSpecification<Student>
        .Create()
        .Include(s => s.Books)
        .ThenInclude(b => b.Author)
        .OrderBy(s => s.Name)
        .OrderBy(s => s.Age);

    private readonly SchoolContext _testDBContext;

    public DBSplitQueryTests(DatabaseFixture fixture)
    {
        Fixture = fixture;
        _testDBContext = fixture.TestDBContext;
    }

    public DatabaseFixture Fixture { get; }

    [Fact]
    public void QueryShouldBeSplit()
    {
        // Arrange
        _testDBContext.DbCommandsLog.Clear();

        // Act
        _ = _testDBContext.BySpecification(_splitTestSpecification.AsSplitQuery()).ToList();

        // Assert
        _testDBContext.DbCommandsLog.Count.Should().Be(2);
    }

    [Fact]
    public void QueryShouldNotBeSplit()
    {
        // Arrange
        _testDBContext.DbCommandsLog.Clear();

        // Act
        _ = _testDBContext.BySpecification(_splitTestSpecification.AsSingleQuery()).ToList();

        // Assert
        _testDBContext.DbCommandsLog.Count.Should().Be(1);
    }
}