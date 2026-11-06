using DimonSmart.Specification.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

[Collection("Database collection")]
public sealed class CriteriaOnlyDbTests(DatabaseFixture fixture)
{
    private readonly SchoolContext _db = fixture.TestDBContext;

    [Fact]
    public async Task CountAndAnyIgnoreSpecificationPagingButFullEvaluationDoesNot()
    {
        var specification = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age >= 22)
            .OrderBy(s => s.Name)
            .Skip(1)
            .Take(1);

        Assert.Equal(2, await _db.Students.BySpecificationCriteria(specification).CountAsync());
        Assert.Equal(2, await _db.BySpecificationCriteria(specification).CountAsync());
        Assert.True(await _db.Students.BySpecificationCriteria(specification.Take(0)).AnyAsync());
        Assert.Equal(1, await _db.Students.BySpecification(specification).CountAsync());
        Assert.False(await _db.Students.BySpecification(specification.Take(0)).AnyAsync());
    }

    [Fact]
    public async Task SqlExcludesOrderPagingSeekIncludesTrackingSplittingAndTags()
    {
        var specification = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age >= 20)
            .OrderBy(s => s.Age)
            .ThenBy(s => s.Id)
            .SeekAfter((22, int.MaxValue))
            .Take(1)
            .Include(s => s.Books)
            .ThenInclude(b => b.Author)
            .AsNoTracking()
            .AsSplitQuery()
            .IgnoreAutoIncludes()
            .TagWith("SPECIFICATION_TAG");

        var criteriaQuery = _db.Students.BySpecificationCriteria(specification);
        var sql = criteriaQuery.Select(s => s.Id).ToQueryString();

        Assert.Contains("WHERE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Age", sql);
        Assert.DoesNotContain("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LIMIT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("OFFSET", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("JOIN", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SPECIFICATION_TAG", sql);
        Assert.DoesNotContain("Seek", criteriaQuery.Expression.ToString());
        Assert.DoesNotContain("Include", criteriaQuery.Expression.ToString());
        Assert.DoesNotContain("AsNoTracking", criteriaQuery.Expression.ToString());
        Assert.DoesNotContain("AsSplitQuery", criteriaQuery.Expression.ToString());
        Assert.Equal(3, await criteriaQuery.CountAsync());

        var fullQuery = _db.Students.BySpecification(specification);
        var fullSql = fullQuery.ToQueryString();
        Assert.Contains("SPECIFICATION_TAG", fullSql);
        Assert.Contains("ORDER BY", fullSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LIMIT", fullSql, StringComparison.OrdinalIgnoreCase);
        var student = Assert.Single(await fullQuery.ToListAsync());
        Assert.Equal(30, student.Age);
        Assert.NotEmpty(student.Books);
        Assert.All(student.Books, book => Assert.NotNull(book.Author));
    }

    [Fact]
    public async Task ExistingQueryableOperationsRemainIntactAndContextOverloadMatches()
    {
        var specification = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age >= 22)
            .OrderByDesc(s => s.Age)
            .Take(0);
        var fromContext = _db.BySpecificationCriteria(specification);
        var fromSet = _db.Students.BySpecificationCriteria(specification);
        Assert.Equal(fromContext.ToQueryString(), fromSet.ToQueryString());
        Assert.Equal(await fromContext.CountAsync(), await fromSet.CountAsync());

        var source = _db.Students.Where(s => s.Age >= 20).OrderBy(s => s.Age).Take(1);
        var query = source.BySpecificationCriteria(specification);
        var sql = query.ToQueryString();
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LIMIT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, await query.CountAsync());
    }

    [Fact]
    public void NamedAndUnnamedGlobalQueryFiltersKeepTheirMembershipSemantics()
    {
        using var db = CreateFilteredContext();
        var specification = EFCoreSpecification.Create<FilterOrder>()
            .Where(o => o.TotalInCents >= 100)
            .OrderByDesc(o => o.Id)
            .Take(0);
        var onlySoftDeletion = specification.IgnoreQueryFilters("SoftDeletionFilter");
        var onlyTenant = specification.IgnoreQueryFilters("TenantFilter");
        var all = specification.IgnoreQueryFilters();

        Assert.Equal(1, db.Orders.BySpecificationCriteria(specification).Count());
        Assert.Equal(2, db.Orders.BySpecificationCriteria(onlySoftDeletion).Count());
        Assert.Equal(2, db.Orders.BySpecificationCriteria(onlyTenant).Count());
        Assert.Equal(4, db.Orders.BySpecificationCriteria(all).Count());
        Assert.Equal(4, db.Orders.BySpecificationCriteria(
            specification.IgnoreQueryFilters("SoftDeletionFilter", "TenantFilter")).Count());
    }

    [Fact]
    public void NamedFilterSqlOnlySuppressesRequestedFilter()
    {
        using var db = CreateFilteredContext();
        var specification = EFCoreSpecification.Create<FilterOrder>()
            .IgnoreQueryFilters("SoftDeletionFilter")
            .Where(o => o.TotalInCents >= 100)
            .OrderBy(o => o.Id)
            .TagWith("NOT_IN_CRITERIA_SQL");

        var sql = db.Orders.BySpecificationCriteria(specification).Select(o => o.Id).ToQueryString();
        Assert.Contains("TenantId", sql);
        Assert.DoesNotContain("IsDeleted", sql);
        Assert.DoesNotContain("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("NOT_IN_CRITERIA_SQL", sql);
        Assert.Equal(2, db.BySpecificationCriteria(specification).Count());
    }

    [Fact]
    public void ComposedCriteriaPreserveFiltersAndExistingCompatibilityChecks()
    {
        var older = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age >= 20)
            .Include(s => s.Books);
        var alex = EFCoreSpecification.Create<Student>()
            .Where(s => s.Name == "Alex")
            .OrderBy(s => s.Id)
            .Take(1);

        Assert.Equal(2, _db.Students.BySpecificationCriteria(older.And(alex)).Count());
        Assert.Equal(3, _db.Students.BySpecificationCriteria(older.Or(alex)).Count());
        Assert.Equal(1, _db.Students.BySpecification(older.And(alex)).Count());
        Assert.Throws<InvalidOperationException>(() =>
            older.And(alex.OrderByDesc(s => s.Age)).EnsureCompatibleWith(alex));
    }

    [Fact]
    public void InvalidQueryableProviderAndNullArgumentsAreRejected()
    {
        IQueryable<Student> nullQuery = null!;
        var specification = EFCoreSpecification.Create<Student>();

        Assert.Equal("query", Assert.Throws<ArgumentNullException>(() =>
            nullQuery.BySpecificationCriteria(specification)).ParamName);
        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(() =>
            _db.Students.BySpecificationCriteria((EFCoreSpecification<Student>)null!)).ParamName);
        Assert.Equal("query", Assert.Throws<ArgumentException>(() =>
            new[] { new Student() }.AsQueryable().BySpecificationCriteria(specification)).ParamName);

        DbContext nullContext = null!;
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            nullContext.BySpecificationCriteria(specification)).ParamName);
        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(() =>
            _db.BySpecificationCriteria((EFCoreSpecification<Student>)null!)).ParamName);
    }

    private static FilterOrdersContext CreateFilteredContext()
    {
        var db = new FilterOrdersContext(1);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        db.Orders.AddRange(
            new FilterOrder { Id = 1, TenantId = 1, TotalInCents = 100 },
            new FilterOrder { Id = 2, TenantId = 1, IsDeleted = true, TotalInCents = 200 },
            new FilterOrder { Id = 3, TenantId = 2, TotalInCents = 300 },
            new FilterOrder { Id = 4, TenantId = 2, IsDeleted = true, TotalInCents = 400 });
        db.SaveChanges();
        db.ChangeTracker.Clear();
        return db;
    }
}
