using DimonSmart.Specification.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

[Collection("Database collection")]
public sealed class StringIncludeDbTests(DatabaseFixture fixture)
{
    private readonly SchoolContext _db = fixture.TestDBContext;

    [Fact]
    public async Task StringPathsLoadNestedReferencesAndCollections()
    {
        var spec = EFCoreSpecification.Create<Student>()
            .Include("Books.Author")
            .Include("School")
            .AsNoTracking();

        Assert.Equal(new[] { "Books.Author", "School" }, spec.EFCoreSpecificationData.Includes);
        var students = await _db.BySpecification(spec).ToListAsync();
        Assert.Equal(3, students.Count);
        Assert.All(students, student =>
        {
            Assert.NotNull(student.School);
            Assert.NotEmpty(student.Books);
            Assert.All(student.Books, book => Assert.NotNull(book.Author));
        });
    }

    [Fact]
    public void StringAndTypedPathsDeduplicateInBothOrders()
    {
        var first = EFCoreSpecification.Create<Student>()
            .Include("Books")
            .Include(s => s.Books)
            .ThenInclude(b => b.Author);
        var second = EFCoreSpecification.Create<Student>()
            .Include(s => s.Books).ThenInclude(b => b.Author)
            .Include("Books");
        Assert.Equal(new[] { "Books.Author" }, first.EFCoreSpecificationData.Includes);
        Assert.Equal(new[] { "Books.Author" }, second.EFCoreSpecificationData.Includes);

        var stringOnly = EFCoreSpecification.Create<Student>().Include("Books.Author");
        Assert.Equal(new[] { "Books.Author" },
            stringOnly.Include(s => s.Books).ThenInclude(b => b.Author)
                .EFCoreSpecificationData.Includes);

        Assert.Equal(_db.Students.AsNoTracking().Include("Books.Author").ToQueryString(),
            _db.BySpecification(stringOnly.AsNoTracking()).ToQueryString());
    }

    [Fact]
    public void StringIncludeComposesAcrossAndOr()
    {
        var books = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age >= 22).Include("Books.Author");
        var school = EFCoreSpecification.Create<Student>()
            .Where(s => s.Name == "Alex").Include(s => s.School);
        var composed = books.And(school).AsNoTracking();

        Assert.Equal(new[] { "Books.Author", "School" }, composed.EFCoreSpecificationData.Includes);
        Assert.Equal(2, _db.BySpecification(composed).Count());
        Assert.Equal(2, _db.BySpecification(books.Or(school)).Count());
        Assert.Single(books.EFCoreSpecificationData.Includes);
        Assert.Single(school.EFCoreSpecificationData.Includes);
    }

    [Fact]
    public void FilteredIncludeConflictIsDetectedBeforeExecution()
    {
        var filtered = EFCoreSpecification.Create<Student>()
            .Include(s => s.Books.Where(b => b.Name.StartsWith("A")));
        Assert.Contains("conflicting Include", Assert.Throws<InvalidOperationException>(
            () => filtered.Include("Books.Author")).Message);
        var unfiltered = EFCoreSpecification.Create<Student>().Include("Books.Author");
        Assert.Contains("conflicting Include", Assert.Throws<InvalidOperationException>(
            () => unfiltered.And(filtered)).Message);
        Assert.Throws<InvalidOperationException>(() =>
            EFCoreSpecification.Create<Student>().Include("Books").Select(s => s.Name));
    }

    [Fact]
    public void InvalidAndDisabledPathsAreValidatedConsistently()
    {
        var root = EFCoreSpecification.Create<Student>();
        Assert.Same(root, root.Include("Books.Author", false));
        Assert.Equal(new[] { "Books.Author" },
            root.Include("Books.Author", true).EFCoreSpecificationData.Includes);

        Assert.Equal("path", Assert.Throws<ArgumentNullException>(
            () => root.Include((string)null!)).ParamName);
        Assert.Equal("path", Assert.Throws<ArgumentException>(
            () => root.Include("")).ParamName);
        Assert.Equal("path", Assert.Throws<ArgumentException>(
            () => root.Include("Books..Author")).ParamName);
        Assert.Equal("path", Assert.Throws<ArgumentException>(
            () => root.Include("Books.Unknown")).ParamName);
        Assert.Equal("path", Assert.Throws<ArgumentException>(
            () => root.Include("Books. Author")).ParamName);
        Assert.Equal("path", Assert.Throws<ArgumentException>(
            () => root.Include("Missing")).ParamName);
        Assert.Empty(root.EFCoreSpecificationData.Includes);
    }
}
