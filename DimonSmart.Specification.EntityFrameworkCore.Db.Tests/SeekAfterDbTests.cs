using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using DimonSmart.Specification.Tests.Common;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

[Collection("Database collection")]
public sealed class SeekAfterDbTests
{
    private readonly SchoolContext _db;
    public SeekAfterDbTests(DatabaseFixture fixture) => _db = fixture.TestDBContext;

    [Fact]
    public void SqliteTranslatesCompositeSeekAndProjection()
    {
        var all = _db.Students.AsNoTracking()
            .OrderByDescending(x => x.Age).ThenBy(x => x.Id).ToList();
        var anchor = all[0];

        var spec = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age >= 18)
            .OrderByDesc(s => s.Age)
            .OrderBy(s => s.Id)
            .SeekAfter((anchor.Age, anchor.Id))
            .Take(20)
            .Select(s => s.Id);

        var query = _db.BySpecification(spec);
        var sql = query.ToQueryString();

        Assert.Contains("WHERE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Age", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Id", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("OFFSET", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(all.Skip(1).Select(x => x.Id), query.ToArray());
    }

    public sealed class Row
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Rank { get; set; }
    }

    public sealed class RowsContext(DbContextOptions<RowsContext> options) : DbContext(options)
    {
        public DbSet<Row> Rows => Set<Row>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<Row>().HasIndex(x => new { x.CreatedAt, x.Rank, x.Id });
    }

    [Fact]
    public void SqliteSupportsDateTimeAndMixedDirectionsAcrossSuccessivePages()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<RowsContext>().UseSqlite(connection).Options;
        using var db = new RowsContext(options);
        db.Database.EnsureCreated();
        var d1 = new DateTime(2026, 1, 1);
        var d2 = new DateTime(2026, 1, 2);
        var d3 = new DateTime(2026, 1, 3);
        db.Rows.AddRange(
            new Row { Id = 1, CreatedAt = d3, Rank = 2 },
            new Row { Id = 2, CreatedAt = d3, Rank = 1 },
            new Row { Id = 3, CreatedAt = d3, Rank = 1 },
            new Row { Id = 4, CreatedAt = d2, Rank = 3 },
            new Row { Id = 5, CreatedAt = d2, Rank = 3 },
            new Row { Id = 6, CreatedAt = d1, Rank = 0 });
        db.SaveChanges();

        var ordered = EFCoreSpecification.Create<Row>()
            .OrderByDesc(r => r.CreatedAt)
            .OrderBy(r => r.Rank)
            .OrderBy(r => r.Id)
            .AsNoTracking();

        var first = db.BySpecification(ordered.Take(2)).ToList();
        Assert.Equal(new[] { 2, 3 }, first.Select(r => r.Id));

        var nextSpec = ordered.SeekAfter((first[^1].CreatedAt, first[^1].Rank, first[^1].Id))
            .Take(2);
        var sql = db.BySpecification(nextSpec).ToQueryString();
        Assert.Contains("WHERE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("OFFSET", sql, StringComparison.OrdinalIgnoreCase);
        var second = db.BySpecification(nextSpec).ToList();
        Assert.Equal(new[] { 1, 4 }, second.Select(r => r.Id));

        var third = db.BySpecification(ordered.SeekAfter((
            second[^1].CreatedAt, second[^1].Rank, second[^1].Id)).Take(2)).ToList();
        Assert.Equal(new[] { 5, 6 }, third.Select(r => r.Id));
    }
}
