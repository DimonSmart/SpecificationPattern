using DimonSmart.Specification.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

/// <summary>Isolated SQLite contexts avoid tracking navigation-fixup interfering with filtered Includes.</summary>
public class TypedIncludeDbTests
{
    [Fact]
    public void ReferenceCollectionAndMultiLevelThenIncludeLoadRealNavigations()
    {
        using var db = Seed();

        var spec = EFCoreSpecification.Create<IncludeBlog>()
            .Include(b => b.MainPost!.Author)
            .Include(b => b.Posts)
            .ThenInclude(p => p.Author)
            .ThenInclude(a => a.Posts)
            .Include(b => b.Posts)
            .ThenInclude(p => p.Remarks)
            .AsNoTracking();

        var blog = db.BySpecification(spec).Single();
        Assert.NotNull(blog.MainPost);
        Assert.NotNull(blog.MainPost.Author);
        Assert.Equal(5, blog.Posts.Count);
        Assert.All(blog.Posts, p =>
        {
            Assert.NotNull(p.Author);
            Assert.NotEmpty(p.Author.Posts);
            Assert.Equal(2, p.Remarks.Count);
        });
    }

    [Fact]
    public void FilteredIncludeExecutesWhereOrderByThenBySkipTakeAndLoadsChildReference()
    {
        using var db = Seed();
        var limit = 2;
        var prefix = "A";

        var spec = EFCoreSpecification.Create<IncludeBlog>()
            .Include(b => b.Posts
                .Where(p => p.Name.StartsWith(prefix))
                .OrderBy(p => p.Name.Length)
                .ThenBy(p => p.Name)
                .Skip(1)
                .Take(limit))
            .ThenInclude(p => p.Author)
            .AsNoTracking();

        var blog = db.BySpecification(spec).Single();
        Assert.Equal(new[] { "Apricot", "Avocado" }, blog.Posts.Select(p => p.Name).OrderBy(n => n));
        Assert.All(blog.Posts, p => Assert.NotNull(p.Author));
    }

    [Fact]
    public void FilteredIncludeSupportsDescendingAndThenByDescending()
    {
        using var db = Seed();
        var spec = EFCoreSpecification.Create<IncludeBlog>()
            .Include(b => b.Posts.OrderByDescending(p => p.Name.Length)
                .ThenByDescending(p => p.Name).Take(2))
            .AsNoTracking();
        var blog = db.BySpecification(spec).Single();

        Assert.Equal(new[] { "Apricot", "Avocado" }, blog.Posts.Select(p => p.Name).OrderBy(n => n));
    }

    [Fact]
    public void FilteredCollectionFollowingThenIncludeIsApplied()
    {
        using var db = Seed();
        var spec = EFCoreSpecification.Create<IncludeBlog>()
            .Include(b => b.Posts)
            .ThenInclude(p => p.Remarks.Where(r => r.Text.StartsWith("Yes"))
                .OrderBy(r => r.Text).Take(1))
            .AsNoTracking();

        var blog = db.BySpecification(spec).Single();
        Assert.Equal(5, blog.Posts.Count);
        Assert.All(blog.Posts, p =>
        {
            var remark = Assert.Single(p.Remarks);
            Assert.StartsWith("Yes", remark.Text);
        });
    }

    [Fact]
    public void FilteredParentBranchingSharesDescriptorAndPreservesBothChildren()
    {
        using var db = Seed();
        var origin = EFCoreSpecification.Create<IncludeBlog>()
            .Include(b => b.Posts.Where(p => p.Name.StartsWith("A")));
        var author = origin.ThenInclude(p => p.Author);
        var publisher = origin.ThenInclude(p => p.Publisher);
        var combined = author.And(publisher).And(origin.Specification).AsNoTracking();

        Assert.Equal(new[] { "Posts.Author", "Posts.Publisher" }, combined.EFCoreSpecificationData.Includes);
        Assert.Equal(new[] { "Posts" }, origin.EFCoreSpecificationData.Includes);

        var blog = db.BySpecification(combined).Single();
        Assert.Equal(3, blog.Posts.Count);
        Assert.All(blog.Posts, p =>
        {
            Assert.NotNull(p.Author);
            Assert.NotNull(p.Publisher);
        });
    }

    [Fact]
    public void CombiningIndependentBranchesAndOrKeepsAllIncludes()
    {
        using var db = Seed();
        var filter = EFCoreSpecification.Create<IncludeBlog>().Where(b => b.Name == "Library")
            .Include(b => b.Posts).ThenInclude(p => p.Author);
        var other = EFCoreSpecification.Create<IncludeBlog>().Where(b => b.Id > 0)
            .Include(b => b.MainPost!.Publisher);

        Assert.NotEmpty(db.BySpecification(filter.And(other).AsNoTracking()).Single().Posts);
        var blog = db.BySpecification(filter.Or(other).AsNoTracking()).Single();
        Assert.NotNull(blog.MainPost?.Publisher);
        Assert.All(blog.Posts, p => Assert.NotNull(p.Author));
    }

    [Fact]
    public void IndependentIncludesDeduplicateLongerAndShorterPaths()
    {
        using var db = Seed();
        var spec = EFCoreSpecification.Create<IncludeBlog>()
            .Include(b => b.Posts)
            .Include(b => b.Posts)
            .ThenInclude(p => p.Author)
            .Include(b => b.Posts);

        Assert.Equal(new[] { "Posts.Author" }, spec.EFCoreSpecificationData.Includes);
        var blog = db.BySpecification(spec.AsNoTracking()).Single();
        Assert.All(blog.Posts, p => Assert.NotNull(p.Author));
    }

    [Fact]
    public void SqlTagsAreSeparateOrderedAndDuplicatesArePreserved()
    {
        using var db = Seed();
        var options = EFCoreSpecification.Create<IncludeBlog>()
            .TagWith("A").TagWith("A").TagWith("B").TagWith("  ");
        var sql = db.BySpecification(options).ToQueryString();

        Assert.StartsWith("-- A\n-- A\n-- B\n", sql);
    }

    [Fact]
    public void CallSiteIsEmittedOnceAlongsideUserTag()
    {
        using var db = Seed();
        var spec = EFCoreSpecification.Create<IncludeBlog>().TagWithCallSite("Marker");
        var sql = db.BySpecification(spec).ToQueryString();
        Assert.StartsWith("-- Marker\n-- CallSiteIsEmittedOnceAlongsideUserTag() -> ", sql);
        Assert.Equal(1, sql.Split("-- Marker", StringSplitOptions.None).Length - 1);
        Assert.Contains(nameof(CallSiteIsEmittedOnceAlongsideUserTag), sql);
        Assert.Contains(nameof(TypedIncludeDbTests) + ".cs", sql);
    }

    private static IncludeTestContext Seed()
    {
        var db = new IncludeTestContext();
        db.Database.OpenConnection();
        db.Database.EnsureCreated();

        var names = new[] { "Apple", "Apricot", "Avocado", "Banana", "Cherry" };
        var posts = names.Select(n => new IncludePost
        {
            Name = n,
            Author = new IncludeAuthor { Name = "Author " + n },
            Publisher = new IncludePublisher { Name = "Publisher " + n },
            Remarks =
            [
                new IncludeRemark { Text = "Yes " + n },
                new IncludeRemark { Text = "No " + n }
            ]
        }).ToList();

        db.Blogs.Add(new IncludeBlog { Name = "Library", Posts = posts, MainPost = posts[0] });
        db.SaveChanges();
        db.ChangeTracker.Clear();
        return db;
    }
}

public sealed class IncludeTestContext : DbContext
{
    public DbSet<IncludeBlog> Blogs => Set<IncludeBlog>();

    protected override void OnConfiguring(DbContextOptionsBuilder options) =>
        options.UseSqlite("Data Source=:memory:");
}

public sealed class IncludeBlog
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public IncludePost? MainPost { get; set; }
    public List<IncludePost> Posts { get; set; } = [];
}

public sealed class IncludePost
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public IncludeAuthor Author { get; set; } = null!;
    public IncludePublisher Publisher { get; set; } = null!;
    public List<IncludeRemark> Remarks { get; set; } = [];
}

public sealed class IncludeAuthor
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<IncludePost> Posts { get; set; } = [];
}

public sealed class IncludePublisher
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public sealed class IncludeRemark
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
}
