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
            .Include(b => b.MainPost!)
            .ThenInclude(p => p.Remarks)
            .ThenInclude(r => r.Reviewer)
            .Include(b => b.Posts)
            .ThenInclude(p => p.Author)
            .Include(b => b.Posts)
            .ThenInclude(p => p.Remarks)
            .AsNoTracking();

        var blog = db.BySpecification(spec).Single();
        Assert.NotNull(blog.MainPost);
        Assert.NotNull(blog.MainPost.Author);
        Assert.All(blog.MainPost.Remarks, r => Assert.NotNull(r.Reviewer));
        Assert.Equal(5, blog.Posts.Count);
        Assert.All(blog.Posts, p =>
        {
            Assert.NotNull(p.Author);
            Assert.Equal(2, p.Remarks.Count);
        });
    }

    [Fact]
    public void FilteredIncludeExecutesWhereOrderByThenBySkipTake()
    {
        using var db = Seed();
        var limit = 2;
        var prefix = "A";

        var spec = EFCoreSpecification.Create<IncludeBlog>()
            .Include(b => b.Posts
                .Where(p => p.Name.StartsWith(prefix))
                .OrderBy(p => p.Name)
                .ThenBy(p => p.Id)
                .Skip(1)
                .Take(limit))
            .AsNoTracking();

        var blog = db.BySpecification(spec).Single();
        Assert.Equal(new[] { "Apricot", "Avocado" }, blog.Posts.Select(p => p.Name).OrderBy(n => n));
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
    public void FilteredPaginationIsPerParentAndPreservesParentsWithNoMatches()
    {
        using var direct = SeedManyBlogs();
        using var wrapped = SeedManyBlogs();

        var expected = direct.Blogs.AsNoTracking()
            .Include(b => b.Posts.Where(p => p.Name.StartsWith("A"))
                .OrderBy(p => p.Name).ThenBy(p => p.Id).Skip(1).Take(2))
            .ToList();

        var specification = EFCoreSpecification.Create<IncludeBlog>()
            .Include(b => b.Posts.Where(p => p.Name.StartsWith("A"))
                .OrderBy(p => p.Name).ThenBy(p => p.Id).Skip(1).Take(2))
            .AsNoTracking();
        var actual = wrapped.BySpecification(specification).ToList();

        AssertSamePosts(expected, actual);
        Assert.Equal(new[] { "A-L-02", "A-L-03" },
            Assert.Single(actual, b => b.Name == "Large").Posts.Select(p => p.Name).OrderBy(n => n));
        Assert.Equal(new[] { "A-S-02" },
            Assert.Single(actual, b => b.Name == "Small").Posts.Select(p => p.Name));
        Assert.Empty(Assert.Single(actual, b => b.Name == "Empty").Posts);
        Assert.Equal(3, actual.Count);
    }

    [Fact]
    public void StandaloneSkipAndTakeMatchDirectEfCorePerParent()
    {
        using var directSkip = SeedManyBlogs();
        using var wrappedSkip = SeedManyBlogs();
        var expectedSkip = directSkip.Blogs.AsNoTracking()
            .Include(b => b.Posts.OrderBy(p => p.Name).ThenBy(p => p.Id).Skip(2)).ToList();
        var actualSkip = wrappedSkip.BySpecification(
            EFCoreSpecification.Create<IncludeBlog>()
                .Include(b => b.Posts.OrderBy(p => p.Name).ThenBy(p => p.Id).Skip(2))
                .AsNoTracking()).ToList();
        AssertSamePosts(expectedSkip, actualSkip);
        Assert.Equal(6, Assert.Single(actualSkip, b => b.Name == "Large").Posts.Count);
        Assert.Empty(Assert.Single(actualSkip, b => b.Name == "Small").Posts);

        using var directTake = SeedManyBlogs();
        using var wrappedTake = SeedManyBlogs();
        var expectedTake = directTake.Blogs.AsNoTracking()
            .Include(b => b.Posts.OrderBy(p => p.Name).ThenBy(p => p.Id).Take(2)).ToList();
        var actualTake = wrappedTake.BySpecification(
            EFCoreSpecification.Create<IncludeBlog>()
                .Include(b => b.Posts.OrderBy(p => p.Name).ThenBy(p => p.Id).Take(2))
                .AsNoTracking()).ToList();
        AssertSamePosts(expectedTake, actualTake);
        Assert.All(actualTake, b => Assert.Equal(2, b.Posts.Count));
    }

    [Fact]
    public void RootWhereIsIndependentOfFilteredInclude()
    {
        using var direct = SeedManyBlogs();
        using var wrapped = SeedManyBlogs();

        var expected = direct.Blogs.AsNoTracking()
            .Where(b => b.Name != "Empty")
            .Include(b => b.Posts.Where(p => p.Name.StartsWith("A"))).ToList();
        var actual = wrapped.BySpecification(
            EFCoreSpecification.Create<IncludeBlog>()
                .Where(b => b.Name != "Empty")
                .Include(b => b.Posts.Where(p => p.Name.StartsWith("A")))
                .AsNoTracking()).ToList();

        AssertSamePosts(expected, actual);
        Assert.Equal(new[] { "Large", "Small" }, actual.Select(b => b.Name).OrderBy(n => n));
    }

    [Fact]
    public void SecondaryOrderingWithTiesMatchesDirectEfCoreInBothDirections()
    {
        using var directAscending = Seed();
        using var wrappedAscending = Seed();
        var expectedAscending = directAscending.Blogs.AsNoTracking()
            .Include(b => b.Posts.OrderBy(p => p.Name.Length)
                .ThenByDescending(p => p.Name).Skip(1).Take(2)).ToList();
        var actualAscending = wrappedAscending.BySpecification(
            EFCoreSpecification.Create<IncludeBlog>()
                .Include(b => b.Posts.OrderBy(p => p.Name.Length)
                    .ThenByDescending(p => p.Name).Skip(1).Take(2))
                .AsNoTracking()).ToList();
        AssertSamePosts(expectedAscending, actualAscending);
        Assert.Equal(new[] { "Banana", "Cherry" },
            Assert.Single(actualAscending).Posts.Select(p => p.Name).OrderBy(n => n));

        using var directDescending = Seed();
        using var wrappedDescending = Seed();
        var expectedDescending = directDescending.Blogs.AsNoTracking()
            .Include(b => b.Posts.OrderByDescending(p => p.Name.Length)
                .ThenBy(p => p.Name).Take(2)).ToList();
        var actualDescending = wrappedDescending.BySpecification(
            EFCoreSpecification.Create<IncludeBlog>()
                .Include(b => b.Posts.OrderByDescending(p => p.Name.Length)
                    .ThenBy(p => p.Name).Take(2))
                .AsNoTracking()).ToList();
        AssertSamePosts(expectedDescending, actualDescending);
        Assert.Equal(new[] { "Apricot", "Avocado" },
            Assert.Single(actualDescending).Posts.Select(p => p.Name).OrderBy(n => n));
    }

    [Fact]
    public void DifferentFiltersConflictOnAncestorEvenWithDifferentChildBranches()
    {
        var author = EFCoreSpecification.Create<IncludeBlog>()
            .Include(b => b.Posts.Take(2)).ThenInclude(p => p.Author);
        var publisher = EFCoreSpecification.Create<IncludeBlog>()
            .Include(b => b.Posts.Take(2)).ThenInclude(p => p.Publisher);

        var and = Assert.Throws<InvalidOperationException>(() => author.And(publisher));
        var or = Assert.Throws<InvalidOperationException>(() => author.Or(publisher));
        Assert.Contains("Posts", and.Message);
        Assert.Contains("different filtered includes", and.Message);
        Assert.Contains("Posts", or.Message);
        Assert.Contains("different filtered includes", or.Message);
    }

    [Fact]
    public void NestedFilteredThenIncludeMatchesDirectEfCore()
    {
        using var direct = Seed();
        using var wrapped = Seed();

        var expected = direct.Blogs.AsNoTracking()
            .Include(b => b.Posts)
            .ThenInclude(p => p.Remarks.Where(r => r.Text.StartsWith("Yes")))
            .ToList();
        var actual = wrapped.BySpecification(
            EFCoreSpecification.Create<IncludeBlog>()
                .Include(b => b.Posts)
                .ThenInclude(p => p.Remarks.Where(r => r.Text.StartsWith("Yes")))
                .AsNoTracking()).ToList();

        AssertSamePosts(expected, actual);
        var expectedPosts = Assert.Single(expected).Posts.OrderBy(p => p.Id).ToList();
        var actualPosts = Assert.Single(actual).Posts.OrderBy(p => p.Id).ToList();
        for (var i = 0; i < expectedPosts.Count; i++)
        {
            Assert.Equal(expectedPosts[i].Remarks.Select(r => r.Id).OrderBy(id => id),
                actualPosts[i].Remarks.Select(r => r.Id).OrderBy(id => id));
            Assert.Single(actualPosts[i].Remarks);
        }
    }

    [Fact]
    public void FilteredParentWithNestedThenIncludeHasProviderParity()
    {
        using var direct = Seed();
        using var wrapped = Seed();

        List<IncludeBlog>? expected = null;
        var directError = Record.Exception(() => expected = direct.Blogs.AsNoTracking()
            .Include(b => b.Posts.OrderBy(p => p.Id).Take(2))
            .ThenInclude(p => p.Remarks).ToList());

        List<IncludeBlog>? actual = null;
        var wrappedError = Record.Exception(() => actual = wrapped.BySpecification(
            EFCoreSpecification.Create<IncludeBlog>()
                .Include(b => b.Posts.OrderBy(p => p.Id).Take(2))
                .ThenInclude(p => p.Remarks)
                .AsNoTracking()).ToList());

        if (directError is not null)
        {
            // SQLite can reject APPLY-based translation: the wrapped query must fail identically.
            Assert.Contains("APPLY", directError.Message, StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(wrappedError);
            Assert.Equal(directError.GetType(), wrappedError.GetType());
            Assert.Contains("APPLY", wrappedError.Message, StringComparison.OrdinalIgnoreCase);
            return;
        }

        Assert.Null(wrappedError);
        AssertSamePosts(expected!, actual!);
        Assert.All(Assert.Single(actual!).Posts, p => Assert.Equal(2, p.Remarks.Count));
    }

    [Fact]
    public void SharedFilteredBranchesSurviveAndOrAndFurtherThenInclude()
    {
        using var direct = Seed();
        using var wrappedAnd = Seed();
        using var wrappedOr = Seed();

        var source = EFCoreSpecification.Create<IncludeBlog>()
            .Include(b => b.Posts.Where(p => p.Name.StartsWith("A")));
        var author = source.ThenInclude(p => p.Author);
        var publisher = source.ThenInclude(p => p.Publisher);
        var remarks = source.ThenInclude(p => p.Remarks).ThenInclude(r => r.Reviewer);
        var and = author.And(publisher).And(remarks).AsNoTracking();
        var or = author.Or(publisher).Or(remarks).AsNoTracking();

        Assert.Equal(new[] { "Posts.Author", "Posts.Publisher", "Posts.Remarks.Reviewer" },
            and.EFCoreSpecificationData.Includes);
        Assert.Equal(and.EFCoreSpecificationData.Includes, or.EFCoreSpecificationData.Includes);
        Assert.Equal(new[] { "Posts" }, source.EFCoreSpecificationData.Includes);
        Assert.Equal(new[] { "Posts.Author" }, author.EFCoreSpecificationData.Includes);
        Assert.Equal(new[] { "Posts.Publisher" }, publisher.EFCoreSpecificationData.Includes);

        var expected = direct.Blogs.AsNoTracking()
            .Include(b => b.Posts.Where(p => p.Name.StartsWith("A"))).ThenInclude(p => p.Author)
            .Include(b => b.Posts.Where(p => p.Name.StartsWith("A"))).ThenInclude(p => p.Publisher)
            .Include(b => b.Posts.Where(p => p.Name.StartsWith("A"))).ThenInclude(p => p.Remarks)
            .ThenInclude(r => r.Reviewer).ToList();
        var actualAnd = wrappedAnd.BySpecification(and).ToList();
        var actualOr = wrappedOr.BySpecification(or).ToList();

        AssertSamePosts(expected, actualAnd);
        AssertSamePosts(expected, actualOr);
        foreach (var blog in actualAnd.Concat(actualOr))
            Assert.All(blog.Posts, p =>
            {
                Assert.NotNull(p.Author);
                Assert.NotNull(p.Publisher);
                Assert.Equal(2, p.Remarks.Count);
                Assert.All(p.Remarks, r => Assert.NotNull(r.Reviewer));
            });
    }

    [Fact]
    public void AbsorbedShortIncludeCanBeExtendedWithoutDroppingExistingBranch()
    {
        using var direct = Seed();
        using var wrapped = Seed();

        var longer = EFCoreSpecification.Create<IncludeBlog>()
            .Include(b => b.Posts).ThenInclude(p => p.Author);
        var withShortPrefix = longer.Include(b => b.Posts);
        var withSibling = withShortPrefix.ThenInclude(p => p.Publisher);

        Assert.Equal(new[] { "Posts.Author" }, withShortPrefix.EFCoreSpecificationData.Includes);
        Assert.Equal(new[] { "Posts.Author", "Posts.Publisher" }, withSibling.EFCoreSpecificationData.Includes);
        Assert.Equal(new[] { "Posts.Author" }, longer.EFCoreSpecificationData.Includes);

        var expected = direct.Blogs.AsNoTracking()
            .Include(b => b.Posts).ThenInclude(p => p.Author)
            .Include(b => b.Posts).ThenInclude(p => p.Publisher).ToList();
        var actual = wrapped.BySpecification(withSibling.AsNoTracking()).ToList();
        AssertSamePosts(expected, actual);
        Assert.All(Assert.Single(actual).Posts, p =>
        {
            Assert.NotNull(p.Author);
            Assert.NotNull(p.Publisher);
        });
    }

    [Fact]
    public void ImmutableSpecificationCanBeReusedAcrossContextsAndSplitQueries()
    {
        var specification = EFCoreSpecification.Create<IncludeBlog>()
            .Include(b => b.Posts.Where(p => p.Name.StartsWith("A")))
            .ThenInclude(p => p.Remarks)
            .AsNoTracking()
            .AsSplitQuery();

        using var direct = Seed();
        using var first = Seed();
        using var second = Seed();

        var expected = direct.Blogs.AsNoTracking().AsSplitQuery()
            .Include(b => b.Posts.Where(p => p.Name.StartsWith("A")))
            .ThenInclude(p => p.Remarks).ToList();
        var result1 = first.BySpecification(specification).ToList();
        var result2 = second.BySpecification(specification).ToList();

        AssertSamePosts(expected, result1);
        AssertSamePosts(expected, result2);
        Assert.Equal(new[] { "Posts.Remarks" }, specification.EFCoreSpecificationData.Includes);
        Assert.All(Assert.Single(result1).Posts, p => Assert.Equal(2, p.Remarks.Count));
        Assert.All(Assert.Single(result2).Posts, p => Assert.Equal(2, p.Remarks.Count));
    }

    [Fact]
    public void SqlTagsAreSeparateOrderedAndDuplicatesArePreserved()
    {
        using var db = Seed();
        var options = EFCoreSpecification.Create<IncludeBlog>()
            .TagWith("A").TagWith("A").TagWith("B").TagWith("  ");
        var sql = db.BySpecification(options).ToQueryString();

        // EF Core itself deduplicates identical SQL tags; the specification
        // retains both in its immutable ordered diagnostic representation.
        Assert.Equal(string.Join(Environment.NewLine, "A", "A", "B"),
            options.EFCoreSpecificationData.Tag);
        Assert.StartsWith("-- A\n-- B\n", sql);
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

    private static void AssertSamePosts(
        IEnumerable<IncludeBlog> expected, IEnumerable<IncludeBlog> actual)
    {
        var left = expected.OrderBy(b => b.Id).ToArray();
        var right = actual.OrderBy(b => b.Id).ToArray();
        Assert.Equal(left.Select(b => b.Id), right.Select(b => b.Id));
        for (var i = 0; i < left.Length; i++)
        {
            Assert.Equal(left[i].Name, right[i].Name);
            Assert.Equal(left[i].Posts.Select(p => p.Id).OrderBy(id => id),
                right[i].Posts.Select(p => p.Id).OrderBy(id => id));
        }
    }

    private static IncludeTestContext SeedManyBlogs()
    {
        var db = new IncludeTestContext();
        db.Database.OpenConnection();
        db.Database.EnsureCreated();

        static IncludePost Post(string name) => new()
        {
            Name = name,
            Author = new IncludeAuthor { Name = "Author " + name },
            Publisher = new IncludePublisher { Name = "Publisher " + name }
        };

        db.Blogs.AddRange(
            new IncludeBlog
            {
                Name = "Large",
                Posts = Enumerable.Range(1, 7).Select(i => Post($"A-L-{i:00}"))
                    .Append(Post("B-L-01")).ToList()
            },
            new IncludeBlog
            {
                Name = "Small",
                Posts = [Post("A-S-01"), Post("A-S-02")]
            },
            new IncludeBlog
            {
                Name = "Empty",
                Posts = [Post("B-E-01"), Post("B-E-02")]
            });
        db.SaveChanges();
        db.ChangeTracker.Clear();
        return db;
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
                new IncludeRemark { Text = "Yes " + n, Reviewer = new IncludePublisher { Name = "Reviewer yes " + n } },
                new IncludeRemark { Text = "No " + n, Reviewer = new IncludePublisher { Name = "Reviewer no " + n } }
            ]
        }).ToList();

        var blog = new IncludeBlog { Name = "Library", Posts = posts };
        db.Blogs.Add(blog);
        db.SaveChanges();
        blog.MainPost = posts[0];
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
    public IncludePublisher Reviewer { get; set; } = null!;
}
