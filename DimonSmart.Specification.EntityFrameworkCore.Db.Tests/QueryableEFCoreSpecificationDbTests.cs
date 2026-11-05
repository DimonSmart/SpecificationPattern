using DimonSmart.Specification.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

[Collection("Database collection")]
public sealed class QueryableEFCoreSpecificationDbTests(DatabaseFixture fixture)
{
    private readonly SchoolContext _db = fixture.TestDBContext;

    private sealed record StudentSummary(int Id, string Name, int Age);

    [Fact]
    public async Task ExistingQueryPreservesUpstreamFilterAndAppliesOptionsAndPaging()
    {
        var source = _db.Students.Where(s => s.Age >= 20);
        var spec = EFCoreSpecification.Create<Student>()
            .Where(s => s.Name == "Alex")
            .OrderByDesc(s => s.Age)
            .Take(1)
            .AsNoTracking()
            .TagWith("ExistingQueryable");

        IQueryable<Student> query = source.BySpecification(spec);
        var sql = query.ToQueryString();

        Assert.Contains("-- ExistingQueryable", sql);
        Assert.Contains("Age", sql);
        Assert.Contains("Name", sql);
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);

        var result = await query.ToListAsync();
        Assert.Equal(30, Assert.Single(result).Age);
    }

    [Fact]
    public async Task QueryableProjectionRespectsUpstreamFiltersAndUsesSqlProjection()
    {
        IQueryable<Student> source = _db.Students.Where(s => s.Name.StartsWith("A"));
        var spec = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age >= 22)
            .OrderByDesc(s => s.Age)
            .Take(1)
            .AsNoTracking()
            .Select(s => new StudentSummary(s.Id, s.Name, s.Age));

        IQueryable<StudentSummary> query = source.BySpecification(spec);
        var sql = query.ToQueryString();
        Assert.Contains("WHERE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);

        var result = await query.ToListAsync();
        Assert.Equal(new StudentSummary(Assert.Single(result).Id, "Alex", 30), result[0]);
    }

    [Fact]
    public void DbSetAndDbContextOverloadsProduceEquivalentQueriesAndProjections()
    {
        var spec = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age >= 22)
            .OrderBy(s => s.Id)
            .AsNoTracking()
            .TagWith("Parity");
        var fromContext = _db.BySpecification(spec);
        var fromSet = _db.Students.BySpecification(spec);

        Assert.Equal(fromContext.ToQueryString(), fromSet.ToQueryString());
        Assert.Equal(fromContext.Select(s => s.Id), fromSet.Select(s => s.Id));

        var projected = spec.Select(s => s.Name);
        Assert.Equal(_db.BySpecification(projected).ToQueryString(),
            _db.Students.BySpecification(projected).ToQueryString());
        Assert.Equal(_db.BySpecification(projected), _db.Students.BySpecification(projected));
    }

    [Fact]
    public void IncludesAndSplitQueryWorkAfterAnExistingFilter()
    {
        var spec = EFCoreSpecification.Create<Student>()
            .Include(s => s.Books)
            .ThenInclude(b => b.Author)
            .AsSplitQuery()
            .AsNoTracking();

        var query = _db.Students.Where(s => s.Age == 20).BySpecification(spec);
        var students = query.ToList();

        var student = Assert.Single(students);
        Assert.Equal("Sofia", student.Name);
        Assert.NotEmpty(student.Books);
        Assert.All(student.Books, book => Assert.NotNull(book.Author));
    }

    [Fact]
    public void TrackingOptionOverridesEarlierSourceTrackingOption()
    {
        _db.ChangeTracker.Clear();

        var spec = EFCoreSpecification.Create<Student>()
            .Where(s => s.Name == "Sofia")
            .AsTracking();
        var student = Assert.Single(_db.Students.AsNoTracking().BySpecification(spec));

        Assert.Same(student, Assert.Single(_db.ChangeTracker.Entries<Student>()).Entity);
        _db.ChangeTracker.Clear();
    }

    [Fact]
    public void SeekAfterAppliesToPrefilteredQueryable()
    {
        var spec = EFCoreSpecification.Create<Student>()
            .OrderBy(s => s.Age)
            .SeekAfter(22)
            .Take(1)
            .AsNoTracking();

        var query = _db.Students.Where(s => s.Name == "Alex")
            .BySpecification(spec);

        Assert.DoesNotContain("OFFSET", query.ToQueryString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(30, Assert.Single(query).Age);
    }

    [Fact]
    public void QueryableOverloadsValidateNullArgumentsAndRejectLinqToObjectsProviders()
    {
        IQueryable<Student> nullQuery = null!;
        var spec = EFCoreSpecification.Create<Student>();
        var projection = spec.Select(s => s.Name);

        Assert.Equal("query", Assert.Throws<ArgumentNullException>(() =>
            nullQuery.BySpecification(spec)).ParamName);
        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(() =>
            _db.Students.BySpecification((EFCoreSpecification<Student, Composable>)null!)).ParamName);
        Assert.Equal("query", Assert.Throws<ArgumentNullException>(() =>
            nullQuery.BySpecification(projection)).ParamName);
        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(() =>
            _db.Students.BySpecification((EFCoreProjectedSpecification<Student, string>)null!)).ParamName);

        var nonEfSource = new[] { new Student() }.AsQueryable();
        Assert.Equal("query", Assert.Throws<ArgumentException>(() =>
            nonEfSource.BySpecification(spec)).ParamName);
        Assert.Equal("query", Assert.Throws<ArgumentException>(() =>
            nonEfSource.BySpecification(projection)).ParamName);
    }
}
