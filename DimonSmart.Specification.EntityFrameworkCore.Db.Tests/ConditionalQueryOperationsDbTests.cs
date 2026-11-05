using DimonSmart.Specification.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

[Collection("Database collection")]
public sealed class ConditionalQueryOperationsDbTests
{
    private readonly SchoolContext _db;

    public ConditionalQueryOperationsDbTests(DatabaseFixture fixture) => _db = fixture.TestDBContext;

    [Fact]
    public void EnabledConditionalOperationsProduceSameSqlAndResults()
    {
        var root = EFCoreSpecification.Create<Student>()
            .Include(s => s.School)
            .Where(s => s.Age >= 20)
            .AsNoTracking()
            .TagWith("conditional paging");

        var conditional = root
            .OrderBy(s => s.Name, true)
            .OrderByDesc(s => s.Age, true)
            .ThenBy(s => s.Id, true)
            .ThenByDesc(s => s.Id, true)
            .Skip(1, true)
            .Take(1, true);
        var ordinary = root
            .OrderBy(s => s.Name)
            .OrderByDesc(s => s.Age)
            .ThenBy(s => s.Id)
            .ThenByDesc(s => s.Id)
            .Skip(1)
            .Take(1);

        var sql = _db.BySpecification(conditional).ToQueryString();
        Assert.Equal(_db.BySpecification(ordinary).ToQueryString(), sql);
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LIMIT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("conditional paging", sql);
        Assert.Equal(new[] { 22 }, _db.BySpecification(conditional).Select(s => s.Age).ToArray());
        Assert.Equal(new[] { "School" }, conditional.EFCoreSpecificationData.Includes);
        Assert.True(conditional.EFCoreSpecificationData.IsAsNoTracking);
    }

    [Fact]
    public void DisabledOperationsPreserveEfOptionsAndConfiguredInstance()
    {
        var root = EFCoreSpecification.Create<Student>()
            .Include(s => s.School)
            .AsNoTracking()
            .TagWith("disabled operations");
        var disabled = root
            .OrderBy(s => s.Name, false)
            .OrderByDesc(s => s.Age, false)
            .ThenBy(s => s.Id, false)
            .ThenByDesc(s => s.Id, false)
            .Skip(100, false)
            .Take(0, false);

        Assert.Same(root, disabled);
        Assert.Empty(disabled.SpecificationData.OrderExpressions);
        Assert.Null(disabled.SpecificationData.SkipQ);
        Assert.Null(disabled.SpecificationData.TakeQ);
        Assert.Equal(_db.BySpecification(root).ToQueryString(),
            _db.BySpecification(disabled).ToQueryString());
        Assert.Equal(new[] { "School" }, disabled.EFCoreSpecificationData.Includes);
        Assert.Equal(3, _db.BySpecification(disabled).Count());
    }

    [Fact]
    public void FalseOnComposableStillProducesConfiguredType()
    {
        var composable = EFCoreSpecification.Create<Student>().Where(s => s.Age >= 22);
        EFCoreSpecification<Student, Configured> configured = composable.OrderBy(s => s.Name, false);

        Assert.NotSame(composable, configured);
        Assert.Empty(configured.SpecificationData.OrderExpressions);
        Assert.Same(configured, configured.Skip(10, false));
        Assert.Same(configured, configured.Take(1, false));
        Assert.Equal(new[] { 22, 30 },
            _db.BySpecification(configured).OrderBy(s => s.Age).Select(s => s.Age).ToArray());
    }

    [Fact]
    public void ThenByWithoutEnabledPrimaryThrowsOnlyWhenEnabled()
    {
        var noPrimary = EFCoreSpecification.Create<Student>().OrderBy(s => s.Name, false);
        Assert.Same(noPrimary, noPrimary.ThenBy(s => s.Id, false));
        Assert.Same(noPrimary, noPrimary.ThenByDesc(s => s.Id, false));
        Assert.Throws<InvalidOperationException>(() => noPrimary.ThenBy(s => s.Id, true));
        Assert.Throws<InvalidOperationException>(() => noPrimary.ThenByDesc(s => s.Id, true));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CoreValidationIsEnforcedThroughEfOverloads(bool condition)
    {
        var root = EFCoreSpecification.Create<Student>();
        Assert.Equal("expression", Assert.Throws<ArgumentNullException>(() =>
            root.OrderBy<int>(null!, condition)).ParamName);
        Assert.Equal("expression", Assert.Throws<ArgumentNullException>(() =>
            root.OrderByDesc<int>(null!, condition)).ParamName);
        Assert.Equal("expression", Assert.Throws<ArgumentNullException>(() =>
            root.ThenBy<int>(null!, condition)).ParamName);
        Assert.Equal("expression", Assert.Throws<ArgumentNullException>(() =>
            root.ThenByDesc<int>(null!, condition)).ParamName);
        Assert.Equal("count", Assert.Throws<ArgumentOutOfRangeException>(() =>
            root.Skip(-1, condition)).ParamName);
        Assert.Equal("count", Assert.Throws<ArgumentOutOfRangeException>(() =>
            root.Take(-1, condition)).ParamName);
    }

    [Fact]
    public void DisabledOperationsDoNotConflictWithSeekOrComposition()
    {
        var seek = EFCoreSpecification.Create<Student>().OrderBy(s => s.Age).SeekAfter(20);
        var unchanged = seek
            .OrderBy(s => s.Name, false)
            .ThenBy(s => s.Id, false)
            .Skip(4, false);

        Assert.Same(seek, unchanged);
        Assert.Equal(new[] { 22, 30 },
            _db.BySpecification(unchanged).Select(s => s.Age).ToArray());
        Assert.Throws<InvalidOperationException>(() => seek.Skip(4, true));
        Assert.Throws<InvalidOperationException>(() => seek.ThenBy(s => s.Id, true));

        var ignored = EFCoreSpecification.Create<Student>()
            .OrderBy(s => s.Name, false)
            .Skip(2, false)
            .Take(1, false);
        Assert.Equal(new[] { 22, 30 },
            _db.BySpecification(seek.And(ignored)).Select(s => s.Age).ToArray());
        Assert.Equal(new[] { 22, 30 },
            _db.BySpecification(seek.Or(ignored)).Select(s => s.Age).ToArray());
    }
}
