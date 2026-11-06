using DimonSmart.Specification.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

[Collection("Database collection")]
public sealed class ReusableProjectionAndConditionalEfDbTests(DatabaseFixture fixture)
{
    private readonly SchoolContext _db = fixture.TestDBContext;

    [Fact]
    public async Task InterfaceTypedEfSpecificationsKeepEfSemantics()
    {
        IEFCoreSpecification<Student> filter = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age >= 22).AsNoTracking().TagWith("interface filter");
        var query = _db.Students.BySpecification(filter);
        Assert.Contains("interface filter", query.ToQueryString());
        Assert.Equal(2, await query.CountAsync());
        Assert.True(filter.EFCoreSpecificationData.IsAsNoTracking);

        IEFCoreSpecification<Student, string> projected = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age >= 22).AsNoTracking().Select(s => s.Name);
        Assert.Equal(new[] { "Alex", "Alex" }, await _db.BySpecification(projected).ToArrayAsync());
        Assert.Equal(2, await _db.BySpecificationCriteria(projected).CountAsync());
    }

    [Fact]
    public void ConditionalIncludeSkipsTheWholeInactiveChain()
    {
        var root = EFCoreSpecification.Create<Student>();
        var inactive = root.Include(s => s.Books, false)
            .ThenInclude(b => b.Author, true);
        Assert.Empty(inactive.EFCoreSpecificationData.Includes);
        Assert.Equal(_db.BySpecification(root).ToQueryString(),
            _db.BySpecification(inactive).ToQueryString());

        var truncated = root.Include(s => s.Books)
            .ThenInclude(b => b.Author, false)
            .ThenInclude(a => a.Id, true);
        Assert.Equal(new[] { "Books" }, truncated.EFCoreSpecificationData.Includes);

        var active = root.Include(s => s.Books, true).ThenInclude(b => b.Author, true);
        Assert.Equal(new[] { "Books.Author" }, active.EFCoreSpecificationData.Includes);
        Assert.NotEmpty(_db.BySpecification(active.AsNoTracking()).First().Books);
        Assert.Empty(root.EFCoreSpecificationData.Includes);
    }

    [Fact]
    public void ConditionalReferenceThenIncludeRetainsParentButStopsChild()
    {
        var root = EFCoreSpecification.Create<Student>();
        var disabled = root.Include(s => s.School, false).ThenInclude(s => s.Name, true);
        Assert.Empty(disabled.EFCoreSpecificationData.Includes);

        var parentOnly = root.Include(s => s.School, true).ThenInclude(s => s.Name, false);
        Assert.Equal(new[] { "School" }, parentOnly.EFCoreSpecificationData.Includes);
        Assert.Equal(_db.BySpecification(root.Include(s => s.School)).ToQueryString(),
            _db.BySpecification(parentOnly).ToQueryString());
    }

    [Fact]
    public void DisabledEfOptionsKeepSameInstanceAndEnabledOnesMatchDirectMethods()
    {
        var root = EFCoreSpecification.Create<Student>();
        Assert.Same(root, root.AsTracking(false));
        Assert.Same(root, root.AsNoTracking(false));
        Assert.Same(root, root.AsNoTrackingWithIdentityResolution(false));
        Assert.Same(root, root.AsSplitQuery(false));
        Assert.Same(root, root.AsSingleQuery(false));
        Assert.Same(root, root.IgnoreAutoIncludes(false));
        Assert.Same(root, root.IgnoreQueryFilters(false));
        Assert.Same(root, root.TagWith("disabled", false));
        Assert.True(root.AsNoTracking(true).EFCoreSpecificationData.IsAsNoTracking);
        Assert.True(root.AsNoTrackingWithIdentityResolution(true).EFCoreSpecificationData.IsAsNoTrackingWithIdentityResolution);
        Assert.True(root.AsSplitQuery(true).EFCoreSpecificationData.IsAsSplitQuery);
        Assert.True(root.AsSingleQuery(true).EFCoreSpecificationData.IsAsSingleQuery);
        Assert.True(root.IgnoreAutoIncludes(true).EFCoreSpecificationData.IsIgnoreAutoIncludes);
        Assert.True(root.IgnoreQueryFilters(true).EFCoreSpecificationData.IsIgnoreQueryFilters);
        Assert.Contains("enabled", _db.BySpecification(root.TagWith("enabled", true)).ToQueryString());
        Assert.Equal("tag", Assert.Throws<ArgumentNullException>(
            () => root.TagWith(null!, false)).ParamName);
    }

    [Fact]
    public async Task ReusedEfProjectionTranslatesAndPreservesQueryOptions()
    {
        var root = EFCoreSpecification.Create<Student>().Where(s => s.Age >= 20)
            .OrderBy(s => s.Id).Take(2).AsNoTracking().TagWith("root tag");
        var projection = EFCoreSpecification.Create<Student>().TagWith("projection tag")
            .Select(s => new { s.Id, s.Name }).Where(s => s.Id > 0);

        var composed = root.WithProjectionOf(projection);
        var query = _db.BySpecification(composed);
        var sql = query.ToQueryString();
        Assert.Contains("root tag", sql);
        Assert.Contains("projection tag", sql);
        Assert.Contains("LIMIT", sql, StringComparison.OrdinalIgnoreCase);
        var actual = await query.Select(s => s.Name).ToArrayAsync();
        var expected = await _db.Students.Where(s => s.Age >= 20)
            .OrderBy(s => s.Id).Take(2)
            .Select(s => new { s.Id, s.Name }).Where(s => s.Id > 0)
            .Select(s => s.Name).ToArrayAsync();
        Assert.Equal(expected, actual);

        var allCriteria = await _db.BySpecificationCriteria(composed).CountAsync();
        Assert.True(allCriteria >= actual.Length);
        Assert.Equal(3, allCriteria);
        Assert.Single(root.PipelineSteps.Where(s => s.Operation == "Take"));
    }

    [Fact]
    public void ComposedProjectionRejectsIncludesAndConflictingRootOptions()
    {
        var projection = EFCoreSpecification.Create<Student>().Select(s => s.Name);
        var include = EFCoreSpecification.Create<Student>().Include(s => s.Books);
        Assert.Throws<InvalidOperationException>(() => include.WithProjectionOf(projection));

        var tracked = EFCoreSpecification.Create<Student>().AsTracking();
        var noTrackingProjection = EFCoreSpecification.Create<Student>().AsNoTracking()
            .Select(s => s.Name);
        Assert.Throws<InvalidOperationException>(() => tracked.WithProjectionOf(noTrackingProjection));
        Assert.Equal("projection", Assert.Throws<ArgumentNullException>(
            () => tracked.WithProjectionOf<string>(null!)).ParamName);
    }
}
