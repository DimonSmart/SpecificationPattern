using System.Text.RegularExpressions;
using DimonSmart.Specification.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

[Collection("Database collection")]
public class ProjectionDbTests
{
    private readonly SchoolContext _db;

    public ProjectionDbTests(DatabaseFixture fixture) => _db = fixture.TestDBContext;

    private sealed record StudentDto(int Id, string Name, string SchoolName);
    private sealed record StudentNameDto(string Name);
    private sealed record BookDto(string Name);
    private sealed record StudentBooksDto(string Name, List<BookDto> Books);
    private sealed record StudentEntityDto(Student Student);

    [Fact]
    public async Task ScalarAndNavigationDtosExecuteOnSqliteWithoutInclude()
    {
        EFCoreProjectedSpecification<Student, StudentDto> specification =
            EFCoreSpecification.Create<Student>()
                .Where(s => s.Age >= 20)
                .Select(s => new StudentDto(s.Id, s.Name, s.School.Name));

        IQueryable<StudentDto> query = _db.BySpecification(specification);
        var students = await query.ToListAsync();

        Assert.Equal(3, students.Count);
        Assert.All(students, student => Assert.NotEqual("", student.SchoolName));
        Assert.Equal(3, await query.CountAsync());
        Assert.NotNull(await query.FirstOrDefaultAsync());
    }

    [Fact]
    public async Task CorrelatedBookCollectionProjectsOnSqlite()
    {
        var specification = EFCoreSpecification.Create<Student>()
            .OrderBy(s => s.Age)
            .Select(s => new StudentBooksDto(
                s.Name,
                s.Books.OrderBy(b => b.Name).Select(b => new BookDto(b.Name)).ToList()));

        var students = await _db.BySpecification(specification).ToListAsync();
        Assert.Equal(3, students.Count);
        Assert.All(students, student => Assert.NotEmpty(student.Books));
        Assert.Contains(students, s => s.Name == "Sofia" &&
            s.Books.Any(book => book.Name == "Pride and Prejudice"));
    }

    [Fact]
    public void MixedOrderingAndPagingApplyBeforeProjection()
    {
        var specification = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age > 18)
            .OrderBy(s => s.Name)
            .ThenByDesc(s => s.Age)
            .Skip(1).Take(1)
            .Select(s => s.Age);

        IQueryable<int> ages = _db.BySpecification(specification);
        Assert.Equal(new[] { 22 }, ages.ToArray());
    }

    [Fact]
    public void ProjectionOnlySelectsRequestedSqlColumns()
    {
        var specification = EFCoreSpecification.Create<Student>()
            .Select(s => new StudentNameDto(s.Name));
        var query = _db.BySpecification(specification);
        var sql = query.ToQueryString();
        var match = Regex.Match(sql, @"\bSELECT\b(?<columns>.*?)\bFROM\b",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        Assert.True(match.Success, sql);
        var columns = match.Groups["columns"].Value;
        Assert.Contains("\"Name\"", columns);
        Assert.DoesNotContain("\"Age\"", columns);
        Assert.DoesNotContain("\"Id\"", columns);
        Assert.Equal(3, query.Count());
    }

    [Fact]
    public void TrackingModesArePreservedForEntityContainingDtos()
    {
        _db.ChangeTracker.Clear();
        var tracked = _db.BySpecification(EFCoreSpecification.Create<Student>()
            .Select(s => new StudentEntityDto(s))).First();
        Assert.Contains(_db.ChangeTracker.Entries<Student>(),
            entry => ReferenceEquals(entry.Entity, tracked.Student));

        _db.ChangeTracker.Clear();
        _ = _db.BySpecification(EFCoreSpecification.Create<Student>()
            .AsNoTracking().Select(s => new StudentEntityDto(s))).First();
        Assert.Empty(_db.ChangeTracker.Entries<Student>());

        _ = _db.BySpecification(EFCoreSpecification.Create<Student>()
            .AsNoTrackingWithIdentityResolution().Select(s => new StudentEntityDto(s))).First();
        Assert.Empty(_db.ChangeTracker.Entries<Student>());
    }

    [Fact]
    public void SplitSingleAndTagsAreRetained()
    {
        var query = EFCoreSpecification.Create<Student>()
            .AsSplitQuery().AsSingleQuery().AsSplitQuery()
            .TagWith("First").TagWith("Second").TagWithCallSite("ProjectionMarker")
            .Select(s => s.Name);
        var sql = _db.BySpecification(query).ToQueryString();
        Assert.Contains("-- First", sql);
        Assert.Contains("-- Second", sql);
        Assert.Contains("-- ProjectionMarker", sql);
        Assert.Contains(nameof(SplitSingleAndTagsAreRetained), sql);
        Assert.Equal(3, _db.BySpecification(query).ToList().Count);

        var single = EFCoreSpecification.Create<Student>()
            .AsSingleQuery().Select(s => s.Name);
        Assert.Equal(3, _db.BySpecification(single).Count());
    }

    [Fact]
    public void ExplicitIncludePlansAreRejectedAtSelectTime()
    {
        const string message = "Select cannot be combined with explicit Include/ThenInclude plans";
        var simple = EFCoreSpecification.Create<Student>().Include(s => s.School);
        Assert.Contains(message, Assert.Throws<InvalidOperationException>(() =>
            simple.Select(s => s.Name)).Message);

        var nested = EFCoreSpecification.Create<Student>().Include(s => s.Books)
            .ThenInclude(b => b.Author);
        Assert.Contains(message, Assert.Throws<InvalidOperationException>(() =>
            nested.Select(s => s.Name)).Message);

        var filtered = EFCoreSpecification.Create<Student>()
            .Include(s => s.Books.Where(b => b.Name.StartsWith("A")));
        Assert.Contains(message, Assert.Throws<InvalidOperationException>(() =>
            filtered.Select(s => s.Name)).Message);

        var independent = EFCoreSpecification.Create<Student>().Where(s => s.Age > 18);
        Assert.Throws<InvalidOperationException>(() =>
            independent.And(simple).Select(s => s.Name));
        Assert.Throws<InvalidOperationException>(() =>
            independent.Or(simple).Select(s => s.Name));

        Assert.Equal(3, _db.BySpecification(EFCoreSpecification.Create<Student>()
            .IgnoreAutoIncludes().Select(s => s.Name)).Count());
    }

    [Fact]
    public void InvalidArgumentsAreRejectedImmediately()
    {
        Assert.Equal("selector", Assert.Throws<ArgumentNullException>(() =>
            EFCoreSpecification.Create<Student>().Select<string>(null!)).ParamName);
        var spec = EFCoreSpecification.Create<Student>().Select(s => s.Name);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            EFCoreSpecificationExtension.BySpecification<Student, string>(null!, spec)).ParamName);
        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(() =>
            EFCoreSpecificationExtension.BySpecification<Student, string>(_db, null!)).ParamName);
    }
}

public class ProjectionOptionDbTests
{
    [Fact]
    public void IgnoreQueryFiltersAndIgnoreAutoIncludesHaveIndependentEffects()
    {
        using var db = new ProjectionOptionContext();
        db.Database.OpenConnection();
        db.Database.EnsureCreated();

        var group = new ProjectionGroup { Name = "Engineering" };
        db.People.AddRange(
            new ProjectionPerson { Name = "Visible", Group = group },
            new ProjectionPerson { Name = "Hidden", IsHidden = true, Group = group });
        db.SaveChanges();
        db.ChangeTracker.Clear();

        var ordinary = EFCoreSpecification.Create<ProjectionPerson>().Select(p => p.Name);
        Assert.Equal(new[] { "Visible" }, db.BySpecification(ordinary).ToArray());

        var all = EFCoreSpecification.Create<ProjectionPerson>()
            .IgnoreQueryFilters().Select(p => p.Name);
        Assert.Equal(2, db.BySpecification(all).Count());

        var unknown = EFCoreSpecification.Create<ProjectionPerson>()
            .IgnoreQueryFilters("UnregisteredFilter").Select(p => p.Name);
        var direct = db.People.IgnoreQueryFilters(new[] { "UnregisteredFilter" })
            .Select(p => p.Name);
        var directError = Record.Exception(() => direct.ToArray());
        var specificationError = Record.Exception(() => db.BySpecification(unknown).ToArray());
        Assert.Equal(directError?.GetType(), specificationError?.GetType());
        if (directError is null)
            Assert.Equal(direct.ToArray(), db.BySpecification(unknown).ToArray());

        var entity = EFCoreSpecification.Create<ProjectionPerson>().Select(p => p);
        var autoSql = db.BySpecification(entity).ToQueryString();
        var noAutoSql = db.BySpecification(EFCoreSpecification.Create<ProjectionPerson>()
            .IgnoreAutoIncludes().Select(p => p)).ToQueryString();
        Assert.Contains("JOIN", autoSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("JOIN", noAutoSql, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class ProjectionOptionContext : DbContext
{
    public DbSet<ProjectionPerson> People => Set<ProjectionPerson>();

    protected override void OnConfiguring(DbContextOptionsBuilder options) =>
        options.UseSqlite("Data Source=:memory:");

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<ProjectionPerson>().HasQueryFilter(p => !p.IsHidden);
        builder.Entity<ProjectionPerson>().Navigation(p => p.Group).AutoInclude();
    }
}

public sealed class ProjectionPerson
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsHidden { get; set; }
    public int GroupId { get; set; }
    public ProjectionGroup Group { get; set; } = null!;
}

public sealed class ProjectionGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
