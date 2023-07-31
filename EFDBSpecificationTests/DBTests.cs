using DimonSmart.EFSpecification;
using DimonSmart.Specification;
using DimonSmart.TestsCommon;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DimonSmart.EFDBSpecificationTests;

[Collection("Database collection")]
public class DBTests : TestsBase
{
    private readonly SchoolContext _testDBContext;

    public DBTests(DatabaseFixture fixture)
    {
        Fixture = fixture;
        _testDBContext = fixture.TestDBContext;
    }

    public DatabaseFixture Fixture { get; }

    [Fact]
    public void SimpleWhereConditionTest()
    {
        // Arrange
        var specification = EFSpecification<Student>
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
        var specification1 = EFSpecification<Student>
            .Create()
            .Where(s => s.Age < 21);

        var specification2 = EFSpecification<Student>
            .Create()
            .Where(s => s.Age == 30);

        var specification = specification1.Or(specification2);

        // Act
        var under21 = _testDBContext.Students.BySpecification(specification).ToList();

        // Assert
        under21.Should().BeEquivalentTo(new List<Student> { Sofia20, Alex30 });
    }

    [Fact]
    public void IncludeTest()
    {
        // Arrange
        var specification = EFSpecification<Student>
            .Create()
            .Where(s => s.Age < 21)
            .Include(s => s.School.MainBook.Author);

        // Act
        var under21 = _testDBContext.BySpecification(specification).AsNoTracking().ToList();

        // Assert
        under21
            .Should()
            .BeEquivalentTo(new List<Student> { Sofia20 }, options => options.Excluding(e => e.Books));
    }
}