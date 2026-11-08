using DimonSmart.Specification.Tests.Common;
using FluentAssertions;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

[Collection("Database collection")]
public class DBWhereTests : TestsBase
{
    private readonly SchoolContext _testDBContext;

    public DBWhereTests(DatabaseFixture fixture) => _testDBContext = fixture.TestDBContext;

    [Fact]
    public void SimpleWhereConditionTest()
    {
        // Arrange
        var specification = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age < 21);

        // Act
        var under21 = _testDBContext.BySpecification(specification).ToList();

        // Assert
        under21.Should().BeEquivalentTo(new List<Student> { Sofia20 });
    }

    [Fact]
    public void WhereWithOrClauseTest()
    {
        // Arrange
        var specification1 = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age < 21);

        var specification2 = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age == 30);

        var specification = specification1.Or(specification2);

        // Act
        var under21 = _testDBContext.BySpecification(specification).ToList();

        // Assert
        under21.Should().BeEquivalentTo(new List<Student> { Sofia20, Alex30 });
    }
}