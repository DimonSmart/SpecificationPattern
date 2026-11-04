using DimonSmart.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DocumentationExamples;

public sealed class NamedFilterOrder
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public bool IsDeleted { get; set; }
    public int TotalInCents { get; set; }
}

public sealed class NamedFilterOrdersContext(
    DbContextOptions<NamedFilterOrdersContext> options, int tenantId) : DbContext(options)
{
    public int TenantId { get; } = tenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NamedFilterOrder>()
            .HasQueryFilter("SoftDeletionFilter", o => !o.IsDeleted)
            .HasQueryFilter("TenantFilter", o => o.TenantId == TenantId);
    }
}

public static class NamedQueryFiltersExample
{
    public static IQueryable<NamedFilterOrder> Query(NamedFilterOrdersContext dbContext)
    {
        var expensive = EFCoreSpecification.Create<NamedFilterOrder>()
            .Where(o => o.TotalInCents > 10000);
        var includeDeleted = EFCoreSpecification.Create<NamedFilterOrder>()
            .IgnoreQueryFilters("SoftDeletionFilter");

        return dbContext.BySpecification(expensive.And(includeDeleted)
            .OrderByDesc(o => o.Id).Take(20));
    }

    public static IQueryable<NamedFilterOrder> QueryEither(NamedFilterOrdersContext dbContext)
    {
        var deleted = EFCoreSpecification.Create<NamedFilterOrder>()
            .Where(o => o.IsDeleted).IgnoreQueryFilters("SoftDeletionFilter");
        var expensive = EFCoreSpecification.Create<NamedFilterOrder>()
            .Where(o => o.TotalInCents > 50000).IgnoreQueryFilters("SoftDeletionFilter");

        return dbContext.BySpecification(deleted.Or(expensive));
    }

    public static IQueryable<int> ProjectIds(NamedFilterOrdersContext dbContext)
    {
        var projection = EFCoreSpecification.Create<NamedFilterOrder>()
            .IgnoreQueryFilters("SoftDeletionFilter")
            .Select(o => o.Id);

        return dbContext.BySpecification(projection);
    }

    public static IQueryable<NamedFilterOrder> QueryWithoutAnyFilters(
        NamedFilterOrdersContext dbContext) =>
        dbContext.BySpecification(EFCoreSpecification.Create<NamedFilterOrder>()
            .IgnoreQueryFilters());
}
