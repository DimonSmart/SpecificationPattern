using DimonSmart.Specification.Tests.Common;
using FluentAssertions;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

[Collection("Database collection")]
public class DBTagQueryTests : TestsBase
{
    private readonly SchoolContext _testDBContext;

    public DBTagQueryTests(DatabaseFixture fixture)
    {
        Fixture = fixture;
        _testDBContext = fixture.TestDBContext;
    }

    public DatabaseFixture Fixture { get; }

    [Fact]
    public void QueryShouldBeTaggedWithOneTag()
    {
        // Arrange
        _testDBContext.DbCommandsLog.Clear();
        const string tag = "Name and Age ordered query";

        // Act
        var taggedSpecification = EFCoreSpecification<Student>
            .Create()
            .OrderBy(s => s.Name)
            .OrderBy(s => s.Age)
            .TagWith(tag);

        _ = _testDBContext.BySpecification(taggedSpecification.AsSplitQuery()).ToList();

        // Assert
        _testDBContext.DbCommandsLog
            .Single()
            .Should()
            .Contain(tag);
    }
}