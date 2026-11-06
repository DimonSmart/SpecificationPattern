using DimonSmart.Specification.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

[Collection("Database collection")]
public sealed class EmptyWhereOrDbTests(DatabaseFixture fixture)
{
    private readonly SchoolContext _db = fixture.TestDBContext;

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OrWithUnfilteredOperandDoesNotGenerateWhere(bool unfilteredOnLeft)
    {
        var filtered = EFCoreSpecification.Create<Student>().Where(s => s.Age >= 22);
        var unfiltered = EFCoreSpecification.Create<Student>();
        var combined = unfilteredOnLeft ? unfiltered.Or(filtered) : filtered.Or(unfiltered);

        Assert.Null(combined.SpecificationData.WhereExpression);
        var query = _db.Students.BySpecification(combined);
        Assert.DoesNotContain("WHERE", query.ToQueryString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(await _db.Students.CountAsync(), await query.CountAsync());
        Assert.Equal(await _db.Students.CountAsync(),
            await _db.Students.BySpecificationCriteria(combined).CountAsync());
    }

    [Fact]
    public async Task UnfilteredOrPreservesCompatibleEfOptionsAndPagination()
    {
        var filtered = EFCoreSpecification.Create<Student>().Where(s => s.Age >= 22);
        var options = EFCoreSpecification.Create<Student>()
            .AsNoTracking().OrderBy(s => s.Age).Take(2).TagWith("EMPTY_WHERE_OR");
        var combined = filtered.Or(options);

        Assert.Null(combined.SpecificationData.WhereExpression);
        var query = _db.Students.BySpecification(combined);
        var sql = query.ToQueryString();
        Assert.DoesNotContain("WHERE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LIMIT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("EMPTY_WHERE_OR", sql);
        Assert.Equal(new[] { 20, 22 }, await query.Select(s => s.Age).ToArrayAsync());
    }

    [Fact]
    public void UnfilteredOrDoesNotBypassEfConfigurationConflicts()
    {
        var filtered = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age >= 22).AsTracking();
        var unfiltered = EFCoreSpecification.Create<Student>().AsNoTracking();

        Assert.Throws<InvalidOperationException>(() => filtered.Or(unfiltered));
        Assert.Throws<InvalidOperationException>(() => unfiltered.Or(filtered));
    }
}
