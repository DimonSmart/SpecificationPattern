using DimonSmart.Specification.Tests.Common;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

[Collection("Database collection")]
public class DBIncludeTests : TestsBase
{
    private readonly SchoolContext _testDBContext;

    public DBIncludeTests(DatabaseFixture fixture) => _testDBContext = fixture.TestDBContext;

    [Fact]
    public void IncludeLoadsNestedNavigationsWithoutTrackingFixup()
    {
        var specification = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age < 21)
            .Include(s => s.School.MainBook.Author)
            .AsNoTracking();

        var student = Assert.Single(_testDBContext.BySpecification(specification).ToList());

        Assert.Equal("Sofia", student.Name);
        Assert.NotSame(Sofia20, student);
        Assert.Equal("High School", student.School.Name);
        Assert.Equal("The Great Gatsby", student.School.MainBook.Name);
        Assert.Equal("F. Scott Fitzgerald", student.School.MainBook.Author.Name);
    }
}
