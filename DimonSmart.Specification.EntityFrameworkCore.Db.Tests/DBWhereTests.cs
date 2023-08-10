using DimonSmart.Specification.Tests.Common;
using FluentAssertions;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

[Collection("Database collection")]
public class DBWhereTests : TestsBase
{
    private readonly SchoolContext _testDBContext;

    public DBWhereTests(DatabaseFixture fixture)
    {
        Fixture = fixture;
        _testDBContext = fixture.TestDBContext;
    }

    public DatabaseFixture Fixture { get; }

    [Fact]
    public void SimpleWhereConditionTest()
    {
        // Arrange
        var specification = EFCoreSpecification<Student>
            .Create()
            .Where(s => s.Age < 21);

        // Act
        var under21 = _testDBContext.Students.BySpecification(specification).ToList();

        // Assert
        under21.Should().BeEquivalentTo(new List<Student> { Sofia20 });
    }

    [Fact]
    public void WhereWithOrClauseTest()
    {
        // Arrange
        var specification1 = EFCoreSpecification<Student>
            .Create()
            .Where(s => s.Age < 21);

        var specification2 = EFCoreSpecification<Student>
            .Create()
            .Where(s => s.Age == 30);

        var specification = specification1.Or(specification2);

        // Act
        var under21 = _testDBContext.Students.BySpecification(specification).ToList();

        // Assert
        under21.Should().BeEquivalentTo(new List<Student> { Sofia20, Alex30 });
    }
}