using Microsoft.EntityFrameworkCore;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

public class NamedQueryFiltersDbTests
{
    private static FilterOrdersContext CreateDatabase(int tenantId = 1)
    {
        var db = new FilterOrdersContext(tenantId);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        db.Orders.AddRange(
            new FilterOrder { Id = 1, TenantId = 1, TotalInCents = 100, Lines = [new FilterOrderLine { Id = 11, Label = "A" }] },
            new FilterOrder { Id = 2, TenantId = 1, IsDeleted = true, IsPriority = true, TotalInCents = 200, Lines = [new FilterOrderLine { Id = 12, Label = "B" }] },
            new FilterOrder { Id = 3, TenantId = 2, IsPriority = true, TotalInCents = 300, Lines = [new FilterOrderLine { Id = 13, Label = "C" }] },
            new FilterOrder { Id = 4, TenantId = 2, IsDeleted = true, TotalInCents = 400, Lines = [new FilterOrderLine { Id = 14, Label = "D" }] });
        db.SaveChanges();
        db.ChangeTracker.Clear();
        return db;
    }

    private static int[] Ids(IQueryable<FilterOrder> query) =>
        query.OrderBy(o => o.Id).Select(o => o.Id).ToArray();

    [Fact]
    public void NamedFiltersMatchDirectEfCoreResultsOnSqlite()
    {
        using var db = CreateDatabase();

        var ordinary = EFCoreSpecification.Create<FilterOrder>();
        var soft = ordinary.IgnoreQueryFilters("SoftDeletionFilter");
        var tenant = ordinary.IgnoreQueryFilters("TenantFilter");
        var both = ordinary.IgnoreQueryFilters("SoftDeletionFilter", "TenantFilter");
        var all = ordinary.IgnoreQueryFilters();

        Assert.Equal(new[] { 1 }, Ids(db.BySpecification(ordinary)));
        Assert.Equal(new[] { 1, 2 }, Ids(db.BySpecification(soft)));
        Assert.Equal(new[] { 1, 3 }, Ids(db.BySpecification(tenant)));
        Assert.Equal(new[] { 1, 2, 3, 4 }, Ids(db.BySpecification(both)));
        Assert.Equal(new[] { 1, 2, 3, 4 }, Ids(db.BySpecification(all)));

        Assert.Equal(Ids(db.Orders.IgnoreQueryFilters(new[] { "SoftDeletionFilter" })),
            Ids(db.BySpecification(soft)));
        Assert.Equal(Ids(db.Orders.IgnoreQueryFilters(new[] { "TenantFilter" })),
            Ids(db.BySpecification(tenant)));
        Assert.Equal(Ids(db.Orders.IgnoreQueryFilters(new[] { "SoftDeletionFilter", "TenantFilter" })),
            Ids(db.BySpecification(both)));

        var sql = db.BySpecification(soft).Select(o => o.Id).ToQueryString();
        Assert.Contains("SELECT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHERE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TenantId", sql);
    }

    [Fact]
    public void NamedFiltersAndQueriesAreEvaluatedAgainstCurrentContextTenant()
    {
        using var db = CreateDatabase(tenantId: 2);
        Assert.Equal(new[] { 3 }, Ids(db.BySpecification(EFCoreSpecification.Create<FilterOrder>())));
        Assert.Equal(new[] { 3, 4 }, Ids(db.BySpecification(
            EFCoreSpecification.Create<FilterOrder>().IgnoreQueryFilters("SoftDeletionFilter"))));
    }

    [Fact]
    public void ConsecutiveCallsAndCompositionMergeNamedFilters()
    {
        using var db = CreateDatabase();
        var chained = EFCoreSpecification.Create<FilterOrder>()
            .IgnoreQueryFilters("SoftDeletionFilter")
            .IgnoreQueryFilters("TenantFilter", "SoftDeletionFilter");
        Assert.Equal(new[] { 1, 2, 3, 4 }, Ids(db.BySpecification(chained)));

        var expensive = EFCoreSpecification.Create<FilterOrder>()
            .Where(o => o.TotalInCents >= 200)
            .IgnoreQueryFilters("SoftDeletionFilter");
        var priority = EFCoreSpecification.Create<FilterOrder>()
            .Where(o => o.IsPriority)
            .IgnoreQueryFilters("TenantFilter");

        Assert.Equal(new[] { 2, 3 }, Ids(db.BySpecification(expensive.And(priority))));
        Assert.Equal(new[] { 2, 3, 4 }, Ids(db.BySpecification(expensive.Or(priority))));
    }

    [Fact]
    public void AllAlwaysDominatesCompositionRegardlessOfOperandOrder()
    {
        using var db = CreateDatabase();
        var all = EFCoreSpecification.Create<FilterOrder>().IgnoreQueryFilters();
        var named = EFCoreSpecification.Create<FilterOrder>().IgnoreQueryFilters("SoftDeletionFilter");

        Assert.Equal(new[] { 1, 2, 3, 4 }, Ids(db.BySpecification(all.And(named))));
        Assert.Equal(new[] { 1, 2, 3, 4 }, Ids(db.BySpecification(named.And(all))));
        Assert.Equal(new[] { 1, 2, 3, 4 }, Ids(db.BySpecification(all.Or(named))));
        Assert.Equal(new[] { 1, 2, 3, 4 }, Ids(db.BySpecification(named.Or(all))));
    }

    [Fact]
    public void WhereOrderingPagingTrackingAndTerminalProjectionContinueToWork()
    {
        using var db = CreateDatabase();
        var specification = EFCoreSpecification.Create<FilterOrder>()
            .IgnoreQueryFilters("SoftDeletionFilter")
            .Where(o => o.TotalInCents >= 100)
            .OrderByDesc(o => o.TotalInCents)
            .Skip(1)
            .Take(1)
            .AsNoTracking();

        Assert.Equal(new[] { 1 }, Ids(db.BySpecification(specification)));
        Assert.Empty(db.ChangeTracker.Entries<FilterOrder>());

        var projected = EFCoreSpecification.Create<FilterOrder>()
            .IgnoreQueryFilters("SoftDeletionFilter")
            .Select(o => o.Id);
        Assert.Equal(new[] { 1, 2 }, db.BySpecification(projected).OrderBy(id => id).ToArray());
    }

    [Fact]
    public void IncludeRemainsCompatibleWithNamedFilters()
    {
        using var db = CreateDatabase();
        var spec = EFCoreSpecification.Create<FilterOrder>()
            .Include(o => o.Lines)
            .IgnoreQueryFilters("SoftDeletionFilter");

        var orders = db.BySpecification(spec).OrderBy(o => o.Id).ToList();
        Assert.Equal(new[] { 1, 2 }, orders.Select(o => o.Id));
        Assert.All(orders, o => Assert.Single(o.Lines));
    }

    [Fact]
    public void QueryableSyntaxRetainsSourcePredicatesAndNamedFilterSecurity()
    {
        using var db = CreateDatabase();

        var includeDeleted = EFCoreSpecification.Create<FilterOrder>()
            .IgnoreQueryFilters("SoftDeletionFilter")
            .Include(o => o.Lines)
            .AsNoTracking();
        var source = db.Orders.Where(o => o.IsPriority);

        var result = source.BySpecification(includeDeleted).OrderBy(o => o.Id).ToList();
        Assert.Equal(new[] { 2 }, result.Select(o => o.Id));
        Assert.All(result, order => Assert.Single(order.Lines));

        var projected = EFCoreSpecification.Create<FilterOrder>()
            .IgnoreQueryFilters("SoftDeletionFilter")
            .Select(o => o.Id);
        Assert.Equal(new[] { 2 }, source.BySpecification(projected).ToArray());

        var ignoreAll = EFCoreSpecification.Create<FilterOrder>().IgnoreQueryFilters();
        Assert.Equal(new[] { 2, 3 },
            source.BySpecification(ignoreAll).OrderBy(o => o.Id).Select(o => o.Id).ToArray());
    }

    [Fact]
    public void UnknownFilterNamesArePassedThroughExactlyAsInEfCore()
    {
        using var db = CreateDatabase();
        var source = db.Orders.IgnoreQueryFilters(new[] { "DoesNotExist" });
        var spec = EFCoreSpecification.Create<FilterOrder>().IgnoreQueryFilters("DoesNotExist");

        var directError = Record.Exception(() => Ids(source));
        var specError = Record.Exception(() => Ids(db.BySpecification(spec)));
        Assert.Equal(directError?.GetType(), specError?.GetType());
        if (directError is null)
            Assert.Equal(Ids(source), Ids(db.BySpecification(spec)));
    }
}

public sealed class FilterOrdersContext(int tenantId) : DbContext
{
    public int TenantId { get; } = tenantId;
    public DbSet<FilterOrder> Orders => Set<FilterOrder>();

    protected override void OnConfiguring(DbContextOptionsBuilder builder) =>
        builder.UseSqlite("Data Source=:memory:");

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<FilterOrder>()
            .HasQueryFilter("SoftDeletionFilter", o => !o.IsDeleted)
            .HasQueryFilter("TenantFilter", o => o.TenantId == TenantId);
        builder.Entity<FilterOrder>()
            .HasMany(o => o.Lines).WithOne().HasForeignKey(l => l.OrderId);
    }
}

public sealed class FilterOrder
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsPriority { get; set; }
    public int TotalInCents { get; set; }
    public List<FilterOrderLine> Lines { get; set; } = [];
}

public sealed class FilterOrderLine
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string Label { get; set; } = "";
}
