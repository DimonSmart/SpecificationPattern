using Microsoft.EntityFrameworkCore;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

public class TrackingDbTests
{
    [Fact]
    public void DefaultModeRespectsContextNoTrackingConfiguration()
    {
        using var db = CreateDatabase(QueryTrackingBehavior.NoTracking);

        var result = db.BySpecification(EFCoreSpecification.Create<TrackingEntity>()).ToList();

        Assert.Single(result);
        Assert.Empty(db.ChangeTracker.Entries<TrackingEntity>());
    }

    [Fact]
    public void AsTrackingOverridesContextNoTrackingConfiguration()
    {
        using var db = CreateDatabase(QueryTrackingBehavior.NoTracking);
        var specification = EFCoreSpecification.Create<TrackingEntity>()
            .Where(e => e.Id == 1)
            .AsTracking();

        var entity = Assert.Single(db.BySpecification(specification).ToList());

        Assert.Same(entity, Assert.Single(db.ChangeTracker.Entries<TrackingEntity>()).Entity);
    }

    [Fact]
    public void AsTrackingSurvivesCompositionAndEntityProjection()
    {
        using var db = CreateDatabase(QueryTrackingBehavior.NoTracking);
        var filter = EFCoreSpecification.Create<TrackingEntity>()
            .Where(e => e.Id == 1);
        var tracking = EFCoreSpecification.Create<TrackingEntity>().AsTracking();
        var projection = filter.And(tracking).Select(e => new { Entity = e });

        var projected = Assert.Single(db.BySpecification(projection).ToList());

        Assert.Same(projected.Entity, Assert.Single(db.ChangeTracker.Entries<TrackingEntity>()).Entity);
    }

    [Fact]
    public void ExplicitNoTrackingOverridesContextTrackingConfiguration()
    {
        using var db = CreateDatabase(QueryTrackingBehavior.TrackAll);
        var specification = EFCoreSpecification.Create<TrackingEntity>().AsNoTracking();

        Assert.Single(db.BySpecification(specification).ToList());
        Assert.Empty(db.ChangeTracker.Entries<TrackingEntity>());
    }

    [Fact]
    public void IdentityResolutionModeStillDoesNotTrackEntities()
    {
        using var db = CreateDatabase(QueryTrackingBehavior.TrackAll);
        var specification = EFCoreSpecification.Create<TrackingEntity>()
            .AsTracking().AsNoTrackingWithIdentityResolution();

        Assert.Single(db.BySpecification(specification).ToList());
        Assert.Empty(db.ChangeTracker.Entries<TrackingEntity>());
    }

    [Fact]
    public void FinalAsTrackingOverridesEarlierNoTracking()
    {
        using var db = CreateDatabase(QueryTrackingBehavior.NoTracking);
        var specification = EFCoreSpecification.Create<TrackingEntity>()
            .AsNoTrackingWithIdentityResolution().AsNoTracking().AsTracking();

        var entity = Assert.Single(db.BySpecification(specification).ToList());

        Assert.Same(entity, Assert.Single(db.ChangeTracker.Entries<TrackingEntity>()).Entity);
    }

    private static TrackingContext CreateDatabase(QueryTrackingBehavior behavior)
    {
        var db = new TrackingContext(behavior);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        db.Entities.Add(new TrackingEntity { Id = 1, Name = "Test" });
        db.SaveChanges();
        db.ChangeTracker.Clear();
        return db;
    }

    private sealed class TrackingContext(QueryTrackingBehavior defaultBehavior) : DbContext
    {
        public DbSet<TrackingEntity> Entities => Set<TrackingEntity>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.UseSqlite("Data Source=:memory:")
                .UseQueryTrackingBehavior(defaultBehavior);
    }

    public sealed class TrackingEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
