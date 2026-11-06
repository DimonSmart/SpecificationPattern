using DimonSmart.Specification.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

[Collection("Database collection")]
public sealed class ComposablePipelineDbTests(DatabaseFixture fixture)
{
    private readonly SchoolContext _db = fixture.TestDBContext;

    [Fact]
    public async Task SqliteTranslatesSelectManyWhereAndResultSelectorWithoutMaterialization()
    {
        var spec = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age >= 20)
            .SelectMany(s => s.Books, (s, b) => new { Student = s.Name, Book = b.Name })
            .Where(x => x.Book.StartsWith("A"))
            .OrderBy(x => x.Student)
            .ThenBy(x => x.Book)
            .Select(x => x.Book);

        var query = _db.Students.BySpecification(spec);
        var sql = query.ToQueryString();
        Assert.Contains("JOIN", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        var actual = await query.ToArrayAsync();

        var expectedSource = await _db.Students.Include(s => s.Books).AsNoTracking().ToListAsync();
        var expected = expectedSource.AsQueryable()
            .Where(s => s.Age >= 20)
            .SelectMany(s => s.Books, (s, b) => new { Student = s.Name, Book = b.Name })
            .Where(x => x.Book.StartsWith("A"))
            .OrderBy(x => x.Student)
            .ThenBy(x => x.Book)
            .Select(x => x.Book).ToArray();

        Assert.Equal(expected, actual);
        Assert.Equal(actual, await _db.BySpecification(spec).ToArrayAsync());
    }

    [Fact]
    public async Task SqliteRespectsTakeBeforeAndAfterSelectMany()
    {
        var before = EFCoreSpecification.Create<Student>()
            .OrderBy(s => s.Id).Take(1)
            .SelectMany(s => s.Books).OrderBy(b => b.Id);
        var after = EFCoreSpecification.Create<Student>()
            .SelectMany(s => s.Books).OrderBy(b => b.Id).Take(1);

        var firstBooks = await _db.Students.BySpecification(before).Select(b => b.Id).ToArrayAsync();
        var firstBook = await _db.Students.BySpecification(after).Select(b => b.Id).ToArrayAsync();

        Assert.True(firstBooks.Length > firstBook.Length);
        Assert.Equal(firstBooks[0], firstBook[0]);
        Assert.Contains("JOIN", _db.Students.BySpecification(before).ToQueryString(),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CriteriaOnlyCountsProjectedRowsAndPreservesExternalPrefix()
    {
        var spec = EFCoreSpecification.Create<Student>()
            .OrderBy(s => s.Id).Take(1)
            .SelectMany(s => s.Books)
            .Where(b => b.Name.StartsWith("A"))
            .Take(1);

        var criteria = _db.Students.BySpecificationCriteria(spec);
        var count = await criteria.CountAsync();
        var expected = await _db.Students.SelectMany(s => s.Books)
            .CountAsync(b => b.Name.StartsWith("A"));
        Assert.Equal(expected, count);
        Assert.Equal(expected, await _db.BySpecificationCriteria(spec).CountAsync());
        Assert.True(await _db.Students.BySpecification(spec).CountAsync() <= count);

        var prefix = _db.Students.OrderBy(s => s.Id).Take(1);
        var prefixed = await prefix.BySpecificationCriteria(spec).CountAsync();
        Assert.True(prefixed <= count);
    }

    [Fact]
    public async Task SqliteTranslatesSeekAfterOnProjectedStage()
    {
        var firstId = await _db.Students.MinAsync(s => s.Id);
        var spec = EFCoreSpecification.Create<Student>()
            .AsNoTracking().TagWith("TYPED_PIPELINE")
            .Select(s => new { s.Id, s.Age })
            .OrderBy(x => x.Id).SeekAfter(firstId).Take(2);

        var query = _db.Students.BySpecification(spec);
        var sql = query.ToQueryString();
        Assert.Contains("TYPED_PIPELINE", sql);
        Assert.Contains("WHERE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        var ids = await query.Select(x => x.Id).ToArrayAsync();
        Assert.Equal(await _db.Students.Where(s => s.Id > firstId)
            .OrderBy(s => s.Id).Take(2).Select(s => s.Id).ToArrayAsync(), ids);
    }

    [Fact]
    public void IncludeIsRejectedForAnySelectManyAndConditionalOperatorsStayImmutable()
    {
        var root = EFCoreSpecification.Create<Student>().Include(s => s.Books);
        Assert.Throws<InvalidOperationException>(() => root.SelectMany(s => s.Books));
        Assert.Throws<InvalidOperationException>(() =>
            root.SelectMany(s => s.Books, (s, b) => b.Id));

        var selected = EFCoreSpecification.Create<Student>().Select(s => s.Age);
        Assert.Same(selected, selected.Where(x => x > 0, false));
        Assert.Same(selected, selected.OrderBy(x => x, false));
        Assert.Same(selected, selected.Take(2, false));
    }
}
