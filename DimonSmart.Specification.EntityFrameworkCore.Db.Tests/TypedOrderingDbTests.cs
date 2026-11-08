using System.Linq.Expressions;
using DimonSmart.Specification.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

[Collection("Database collection")]
public sealed class TypedOrderingDbTests
{
    private readonly SchoolContext _db;

    public TypedOrderingDbTests(DatabaseFixture fixture) => _db = fixture.TestDBContext;

    [Fact]
    public void SqliteTranslatesTypedMultiLevelOrderingWithNavigationAndInclude()
    {
        var spec = EFCoreSpecification.Create<Student>()
            .Include(s => s.School)
            .Where(s => s.Age >= 20)
            .OrderBy(s => s.School.Name)
            .ThenByDesc(s => s.Age)
            .ThenBy(s => s.Name)
            .AsNoTracking()
            .AsSingleQuery();

        Assert.Equal(new[] { typeof(string), typeof(int), typeof(string) },
            spec.OrderingSteps().Select(item => item.Expressions[0].ReturnType));
        var query = _db.BySpecification(spec);
        var sql = query.ToQueryString();
        var position = sql.IndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase);
        Assert.True(position >= 0, sql);
        var clause = sql[position..];
        Assert.Contains("Name", clause, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Age", clause, StringComparison.OrdinalIgnoreCase);
        Assert.True(clause.Count(c => c == ',') >= 2, sql);

        Assert.Equal(new[] { 22, 30, 20 },
            query.Select(s => s.Age).ToArray());
    }

    [Fact]
    public void SqlitePreservesThreeDifferentKeyTypesAndProjectionWithPaging()
    {
        var spec = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age >= 20)
            .OrderBy(s => s.Name)
            .ThenByDesc(s => s.Age)
            .ThenBy(s => (long)s.Id)
            .Skip(1)
            .Take(1)
            .Select(s => s.Age);

        var query = _db.BySpecification(spec);
        var sql = query.ToQueryString();
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(new[] { 22 }, query.ToArray());

        var ordering = EFCoreSpecification.Create<Student>()
            .OrderBy(s => s.Name).ThenByDesc(s => s.Age).ThenBy(s => (long)s.Id);
        Assert.Equal(new[] { typeof(string), typeof(int), typeof(long) },
            ordering.OrderingSteps().Select(e => e.Expressions[0].ReturnType));
        Assert.Equal(new[] { 30, 22, 20 },
            _db.BySpecification(ordering).Select(s => s.Age).ToArray());
    }

    [Fact]
    public void NullSelectorsFailImmediatelyAndDiagnosticsCannotChangeEFQueries()
    {
        Assert.Equal("expression", Assert.Throws<ArgumentNullException>(() =>
            EFCoreSpecification.Create<Student>().OrderBy<int>(null!)).ParamName);
        Assert.Equal("expression", Assert.Throws<ArgumentNullException>(() =>
            EFCoreSpecification.Create<Student>().OrderByDesc<int>(null!)).ParamName);

        var spec = EFCoreSpecification.Create<Student>().OrderBy(s => s.Age);
        var diagnostic = spec.OrderingSteps();
        var editable = Assert.IsAssignableFrom<IList<QueryPipelineStep>>(diagnostic);
        Assert.Throws<NotSupportedException>(() => editable.Clear());
        Assert.Equal(new[] { 20, 22, 30 },
            _db.BySpecification(spec).Select(s => s.Age).ToArray());
    }
    [Fact]
    public void SqliteThenByBuildsMixedDirectionOrderingWithTies()
    {
        var spec = EFCoreSpecification.Create<Student>()
            .OrderBy(s => s.Name)
            .ThenByDesc(s => s.Age)
            .ThenBy(s => s.Id)
            .AsNoTracking();

        var query = _db.BySpecification(spec);
        var sql = query.ToQueryString();
        var orderClause = sql[sql.IndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase)..];
        Assert.Contains("Age", orderClause, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DESC", orderClause, StringComparison.OrdinalIgnoreCase);
        Assert.True(orderClause.Count(c => c == ',') >= 2, sql);
        Assert.Equal(new[] { 30, 22, 20 }, query.Select(s => s.Age).ToArray());
        Assert.Equal(new[] { typeof(string), typeof(int), typeof(int) },
            spec.OrderingSteps().Select(x => x.Expressions[0].ReturnType));
    }

    [Fact]
    public void SqliteThenBySupportsNavigationIncludeAndConditionalWhere()
    {
        var spec = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age >= 20, true)
            .Include(s => s.School)
            .OrderBy(s => s.Name)
            .ThenBy(s => s.School.Name)
            .ThenByDesc(s => s.Age)
            .AsNoTracking();

        var query = _db.BySpecification(spec);
        var sql = query.ToQueryString();
        Assert.Contains("JOIN", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(new[] { 22, 30, 20 }, query.Select(s => s.Age).ToArray());
        Assert.Equal(new[] { "Elementary School", "High School", "High School" },
            query.Select(s => s.School.Name).ToArray());
    }

    [Fact]
    public void SqliteThenByPrecedesTakeAndTerminalProjection()
    {
        var spec = EFCoreSpecification.Create<Student>()
            .OrderBy(s => s.Name)
            .ThenByDesc(s => s.Age)
            .Take(1)
            .Select(s => new { s.Name, s.Age });

        Assert.Throws<InvalidOperationException>(() =>
            EFCoreSpecification.Create<Student>().OrderBy(s => s.Name).Take(1).ThenByDesc(s => s.Age));

        var query = _db.BySpecification(spec);
        var sql = query.ToQueryString();
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LIMIT", sql, StringComparison.OrdinalIgnoreCase);
        var student = Assert.Single(query.ToArray());
        Assert.Equal("Alex", student.Name);
        Assert.Equal(30, student.Age);
    }

    [Fact]
    public void SqliteThenByWorksWithCompositeSeekAfter()
    {
        var anchorId = _db.Students.Single(s => s.Age == 30).Id;
        var spec = EFCoreSpecification.Create<Student>()
            .OrderByDesc(s => s.Age)
            .ThenBy(s => s.Id)
            .SeekAfter((30, anchorId))
            .Take(20)
            .AsNoTracking();

        var query = _db.BySpecification(spec);
        var sql = query.ToQueryString();
        Assert.Contains("WHERE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("OFFSET", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(new[] { 22, 20 }, query.Select(s => s.Age).ToArray());
    }

    [Fact]
    public void EfCoreThenByRejectsMissingPrimaryNullKeysAndPostSeekOrdering()
    {
        Assert.Contains("existing OrderBy", Assert.Throws<InvalidOperationException>(() =>
            EFCoreSpecification.Create<Student>().AsNoTracking().ThenBy(s => s.Id)).Message);
        Assert.Throws<InvalidOperationException>(() =>
            EFCoreSpecification.Create<Student>().Take(1).ThenByDesc(s => s.Age));
        Assert.Equal("expression", Assert.Throws<ArgumentNullException>(() =>
            EFCoreSpecification.Create<Student>().ThenBy<int>(null!)).ParamName);
        Assert.Equal("expression", Assert.Throws<ArgumentNullException>(() =>
            EFCoreSpecification.Create<Student>().OrderBy(s => s.Id).ThenByDesc<int>(null!)).ParamName);

        var root = EFCoreSpecification.Create<Student>().OrderBy(s => s.Id);
        var seek = root.SeekAfter(1);
        Assert.Throws<InvalidOperationException>(() => seek.ThenBy(s => s.Age));
        Assert.Throws<InvalidOperationException>(() => seek.ThenByDesc(s => s.Age));
        Assert.Single(root.OrderingSteps());
    }

    [Fact]
    public void EfCoreThenByAfterCompositionKeepsOrderingAndRejectsConflicts()
    {
        var filter = EFCoreSpecification.Create<Student>().Where(s => s.Age >= 20);
        var ordering = EFCoreSpecification.Create<Student>()
            .OrderBy(s => s.Name).ThenBy(s => s.Id);
        var spec = filter.And(ordering).ThenByDesc(s => s.Age);
        var specOr = filter.Or(ordering).ThenByDesc(s => s.Age);

        Assert.Equal(3, spec.OrderingSteps().Count);
        Assert.Equal(3, specOr.OrderingSteps().Count);
        Assert.Throws<InvalidOperationException>(() =>
            spec.And(EFCoreSpecification.Create<Student>().OrderBy(s => s.Age)));
        Assert.Equal(3, _db.BySpecification(spec).Count());
        Assert.Equal(3, _db.BySpecification(specOr).Count());
    }
}
