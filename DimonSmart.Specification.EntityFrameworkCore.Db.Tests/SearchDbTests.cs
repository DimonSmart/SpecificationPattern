using DimonSmart.Specification.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

[Collection("Database collection")]
public sealed class SearchDbTests(DatabaseFixture fixture)
{
    private readonly SchoolContext _db = fixture.TestDBContext;

    [Fact]
    public async Task SearchTranslatesLikeToSqlAndCombinesWithWhereOrderingAndCriteriaOnly()
    {
        var specification = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age >= 20)
            .Search(s => s.Name, "A%")
            .OrderByDesc(s => s.Age)
            .Take(1);

        var query = _db.BySpecification(specification);
        var sql = query.ToQueryString();

        Assert.Contains("LIKE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(new[] { 30 }, await query.Select(s => s.Age).ToArrayAsync());
        Assert.Equal(2, await _db.BySpecificationCriteria(specification).CountAsync());
        Assert.Equal(new[] { "Where", "Where", "OrderByDesc", "Take" },
            specification.PipelineSteps.Select(step => step.Operation));
    }

    [Fact]
    public async Task SearchSupportsSqlWildcardsAndAndOrComposition()
    {
        var alex = EFCoreSpecification.Create<Student>().Search(s => s.Name, "A_ex");
        var sofia = EFCoreSpecification.Create<Student>().Where(s => s.Name == "Sofia");

        Assert.Equal(2, await _db.BySpecification(alex).CountAsync());
        Assert.Equal(3, await _db.BySpecification(alex.Or(sofia)).CountAsync());
        Assert.Equal(0, await _db.BySpecification(alex.And(sofia)).CountAsync());
        Assert.Equal(0, await _db.BySpecification(
            EFCoreSpecification.Create<Student>().Search(s => s.Name, "X%")).CountAsync());
    }

    [Fact]
    public async Task SearchWorksAfterProjectionAndRetainsSqlTranslation()
    {
        var specification = EFCoreSpecification.Create<Student>()
            .Select(s => new { s.Id, s.Name })
            .Search(x => x.Name, "%lex")
            .OrderBy(x => x.Id);

        var query = _db.BySpecification(specification);
        Assert.Contains("LIKE", query.ToQueryString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, await query.CountAsync());
        Assert.Equal(2, await _db.BySpecificationCriteria(specification).CountAsync());
    }

    [Fact]
    public void ConditionalSearchIsImmutableAndValidatesArguments()
    {
        var root = EFCoreSpecification.Create<Student>();
        var unchanged = root.Search(s => s.Name, "A%", false);
        Assert.Same(root, unchanged);
        Assert.Same(root, root.Search(s => s.Name, "A%", false));
        Assert.Equal(3, _db.BySpecification(root).Count());
        Assert.Equal(2, _db.BySpecification(root.Search(s => s.Name, "A%", true)).Count());

        var projected = root.Select(s => new { s.Name });
        Assert.Same(projected, projected.Search(s => s.Name, "%", false));
        Assert.Equal(2, _db.BySpecification(projected.Search(s => s.Name, "A%", true)).Count());
        Assert.Equal("selector", Assert.Throws<ArgumentNullException>(
            () => root.Search(null!, "A%", false)).ParamName);
        Assert.Equal("pattern", Assert.Throws<ArgumentNullException>(
            () => root.Search(s => s.Name, null!, false)).ParamName);
        Assert.Equal("selector", Assert.Throws<ArgumentNullException>(
            () => projected.Search(null!, "A%")).ParamName);
        Assert.Equal("pattern", Assert.Throws<ArgumentNullException>(
            () => projected.Search(s => s.Name, null!)).ParamName);
    }
}
