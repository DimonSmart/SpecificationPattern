using DimonSmart.Specification.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

[Collection("Database collection")]
public class ConditionalWhereDbTests : TestsBase
{
    private readonly SchoolContext _db;

    public ConditionalWhereDbTests(DatabaseFixture fixture) => _db = fixture.TestDBContext;

    [Fact]
    public void TrueMatchesOrdinaryWhereInSqlAndResults()
    {
        var source = EFCoreSpecification.Create<Student>().Where(s => s.Age >= 20);
        var conditional = source.Where(s => s.Name == "Alex", true);
        var ordinary = source.Where(s => s.Name == "Alex");

        var conditionalQuery = _db.BySpecification(conditional);
        Assert.Equal(_db.BySpecification(ordinary).ToQueryString(), conditionalQuery.ToQueryString());
        Assert.Contains("WHERE", conditionalQuery.ToQueryString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AND", conditionalQuery.ToQueryString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(new[] { 22, 30 },
            conditionalQuery.OrderBy(s => s.Age).Select(s => s.Age).ToArray());
        Assert.Equal(new[] { 20, 22, 30 },
            _db.BySpecification(source).OrderBy(s => s.Age).Select(s => s.Age).ToArray());
    }

    [Fact]
    public void FalseReturnsSameInstanceAndDoesNotAddSqlPredicate()
    {
        var source = EFCoreSpecification.Create<Student>().Where(s => s.Age >= 22);
        var skipped = source.Where(s => s.Name == "Sofia", false);

        Assert.Same(source, skipped);
        Assert.Equal(_db.BySpecification(source).ToQueryString(),
            _db.BySpecification(skipped).ToQueryString());
        Assert.Equal(new[] { 22, 30 },
            _db.BySpecification(skipped).OrderBy(s => s.Age).Select(s => s.Age).ToArray());

        var empty = EFCoreSpecification.Create<Student>();
        Assert.Same(empty, empty.Where(s => s.Age < 0, false));
        Assert.Null(empty.SpecificationData.WhereExpression);
    }

    [Fact]
    public void MultipleOptionalPredicatesAreAppliedOnlyWhenEnabled()
    {
        var specification = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age >= 20)
            .Where(s => s.Name == "Sofia", false)
            .Where(s => s.Age < 30, true)
            .Where(s => s.Name == "Alex", true)
            .OrderBy(s => s.Age);

        Assert.Equal(new[] { 22 },
            _db.BySpecification(specification).Select(s => s.Age).ToArray());
    }

    [Fact]
    public void BuildingAndTranslatingQueryDoesNotExecuteIt()
    {
        _db.DbCommandsLog.Clear();
        var specification = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age >= 21, true)
            .Where(s => s.Name == "Nobody", false);
        var query = _db.BySpecification(specification);

        Assert.Empty(_db.DbCommandsLog);
        Assert.Contains("WHERE", query.ToQueryString(), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(_db.DbCommandsLog);

        Assert.Equal(2, query.Count());
        Assert.NotEmpty(_db.DbCommandsLog);
    }

    [Fact]
    public void EfCoreOptionsAndIncludePlansSurviveBothConditionalPaths()
    {
        var source = EFCoreSpecification.Create<Student>()
            .Include(s => s.School)
            .AsNoTracking()
            .AsSplitQuery()
            .IgnoreAutoIncludes()
            .TagWith("conditional where");
        EFCoreSpecification<Student> skipped = source.Where(s => s.Age < 0, false);
        EFCoreSpecification<Student> active = skipped.Where(s => s.Age > 20, true);

        Assert.Same(source, skipped);
        Assert.True(active.EFCoreSpecificationData.IsAsNoTracking);
        Assert.True(active.EFCoreSpecificationData.IsAsSplitQuery);
        Assert.True(active.EFCoreSpecificationData.IsIgnoreAutoIncludes);
        Assert.Equal(new[] { "School" }, active.EFCoreSpecificationData.Includes);
        Assert.Contains("conditional where", _db.BySpecification(active).ToQueryString());
        Assert.Equal(new[] { 22, 30 },
            _db.BySpecification(active).OrderBy(s => s.Age).Select(s => s.Age).ToArray());
    }

    [Fact]
    public void AndAndOrPreserveAbsentFilterSemantics()
    {
        var empty = EFCoreSpecification.Create<Student>().Where(s => s.Age < 0, false);
        var named = EFCoreSpecification.Create<Student>().Where(s => s.Name == "Alex");

        Assert.Equal(new[] { 20, 22, 30 },
            _db.BySpecification(empty.Or(named)).OrderBy(s => s.Age).Select(s => s.Age).ToArray());
        Assert.Equal(new[] { 22, 30 },
            _db.BySpecification(empty.And(named)).OrderBy(s => s.Age).Select(s => s.Age).ToArray());
    }

    [Fact]
    public void NestedLambdasStillTranslateToSql()
    {
        var specification = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age > 20)
            .Where(student => student.Books.Any(book => book.Name == "The Great Gatsby"), true);

        Assert.Contains("EXISTS", _db.BySpecification(specification).ToQueryString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(new[] { 22 },
            _db.BySpecification(specification).Select(s => s.Age).ToArray());
    }

    [Fact]
    public void DocumentedOptionalFiltersExecuteOnSqlite()
    {
        var withoutOptions = DocumentationExamples.ConditionalWhereExample.Query(_db, null, null);
        var withOptions = DocumentationExamples.ConditionalWhereExample.Query(_db, "Alex", 22);

        Assert.Equal(new[] { 20, 22, 30 },
            withoutOptions.Select(s => s.Age).OrderBy(a => a).ToArray());
        Assert.Equal(new[] { 22 }, withOptions.Select(s => s.Age).ToArray());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NullPredicateIsAlwaysRejected(bool condition)
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            EFCoreSpecification.Create<Student>().Where(null!, condition));

        Assert.Equal("predicate", exception.ParamName);
    }
}
