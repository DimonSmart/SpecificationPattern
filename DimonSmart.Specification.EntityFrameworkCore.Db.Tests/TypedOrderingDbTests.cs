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
            .OrderByDesc(s => s.Age)
            .OrderBy(s => s.Name)
            .AsNoTracking()
            .AsSingleQuery();

        Assert.Equal(new[] { typeof(string), typeof(int), typeof(string) },
            spec.SpecificationData.OrderExpressions.Select(item => item.expr.ReturnType));
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
            .OrderByDesc(s => s.Age)
            .OrderBy(s => (long)s.Id)
            .Skip(1)
            .Take(1)
            .Select(s => s.Age);

        var query = _db.BySpecification(spec);
        var sql = query.ToQueryString();
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(new[] { 22 }, query.ToArray());

        var ordering = EFCoreSpecification.Create<Student>()
            .OrderBy(s => s.Name).OrderByDesc(s => s.Age).OrderBy(s => (long)s.Id);
        Assert.Equal(new[] { typeof(string), typeof(int), typeof(long) },
            ordering.SpecificationData.OrderExpressions.Select(e => e.expr.ReturnType));
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
        var diagnostic = spec.SpecificationData.OrderExpressions;
        var editable = Assert.IsAssignableFrom<IList<(OrderDirectionEnum direction, LambdaExpression expr)>>(diagnostic);
        Assert.Throws<NotSupportedException>(() => editable.Clear());
        Assert.Equal(new[] { 20, 22, 30 },
            _db.BySpecification(spec).Select(s => s.Age).ToArray());
    }
}
