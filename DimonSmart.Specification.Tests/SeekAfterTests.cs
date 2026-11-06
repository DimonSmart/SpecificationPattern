using DimonSmart.Specification.Tests.Common;

namespace DimonSmart.Specification.Tests;

public sealed class SeekAfterTests
{
    private sealed class Item
    {
        public int Id { get; init; }
        public int Rank { get; init; }
        public long Sequence { get; init; }
        public decimal Amount { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateOnly Day { get; init; }
        public string Name { get; init; } = "";
        public int? Optional { get; init; }
    }

    private static readonly DateTime D1 = new(2026, 1, 1);
    private static readonly DateTime D2 = new(2026, 1, 2);
    private static readonly DateTime D3 = new(2026, 1, 3);

    private static readonly Item[] Items =
    {
        new() { Id = 5, CreatedAt = D2, Rank = 3, Sequence = 1 },
        new() { Id = 2, CreatedAt = D3, Rank = 1, Sequence = 2 },
        new() { Id = 6, CreatedAt = D1, Rank = 0, Sequence = 3 },
        new() { Id = 1, CreatedAt = D3, Rank = 2, Sequence = 4 },
        new() { Id = 4, CreatedAt = D2, Rank = 3, Sequence = 5 },
        new() { Id = 3, CreatedAt = D3, Rank = 1, Sequence = 6 }
    };

    [Fact]
    public void SingleKeyIsExclusiveAndWorksWithTakeAndProjection()
    {
        var spec = Specification.Create<Item>()
            .OrderBy(x => x.Id)
            .SeekAfter(3)
            .Take(2)
            .Select(x => x.Id);

        Assert.Equal(new[] { 4, 5 }, Items.AsQueryable().BySpecification(spec).ToArray());
    }

    [Fact]
    public void MultiKeyWithMixedDirectionsSkipsAllRowsAtOrBeforeCursor()
    {
        var spec = Specification.Create<Item>()
            .OrderByDesc(x => x.CreatedAt)
            .ThenBy(x => x.Rank)
            .ThenBy(x => x.Id)
            .SeekAfter((D3, 1, 3))
            .Take(3);

        Assert.Equal(new[] { 1, 4, 5 },
            Items.AsQueryable().BySpecification(spec).Select(x => x.Id).ToArray());
    }

    [Fact]
    public void CanFetchEveryPageWithoutDuplicatesOrGapsWhenDataIsUnchanged()
    {
        var ordering = Specification.Create<Item>()
            .OrderByDesc(x => x.CreatedAt)
            .ThenBy(x => x.Rank)
            .ThenBy(x => x.Id);

        var ids = new List<int>();
        Item? last = null;

        for (var iteration = 0; iteration < 10; iteration++)
        {
            var pageSpec = last is null
                ? ordering.Take(2)
                : ordering.SeekAfter((last.CreatedAt, last.Rank, last.Id)).Take(2);
            var page = Items.AsQueryable().BySpecification(pageSpec).ToArray();
            if (page.Length == 0)
                break;

            ids.AddRange(page.Select(x => x.Id));
            last = page[^1];
        }

        Assert.Equal(new[] { 2, 3, 1, 4, 5, 6 }, ids);
    }

    [Fact]
    public void HandlesLongDecimalAndDateOnlyKeys()
    {
        Assert.Equal(new[] { 3L, 2L, 1L },
            Items.AsQueryable().BySpecification(Specification.Create<Item>()
                .OrderByDesc(x => x.Sequence).SeekAfter(4L))
                .Select(x => x.Sequence).ToArray());

        var rows = new[]
        {
            new Item { Id = 1, Amount = 1.5m, Day = new DateOnly(2026, 1, 1) },
            new Item { Id = 2, Amount = 2.5m, Day = new DateOnly(2026, 1, 2) },
            new Item { Id = 3, Amount = 3.5m, Day = new DateOnly(2026, 1, 3) }
        }.AsQueryable();

        Assert.Equal(new[] { 2, 3 },
            rows.BySpecification(Specification.Create<Item>()
                .OrderBy(x => x.Amount).SeekAfter(1.5m)).Select(x => x.Id).ToArray());
        Assert.Equal(new[] { 3 },
            rows.BySpecification(Specification.Create<Item>()
                .OrderBy(x => x.Day).SeekAfter(new DateOnly(2026, 1, 2)))
                .Select(x => x.Id).ToArray());
    }

    [Fact]
    public void OrCombinesOrdinaryFiltersBeforeApplyingSeekToEntireResult()
    {
        var left = Specification.Create<Item>().Where(x => x.Id == 1);
        var right = Specification.Create<Item>()
            .Where(x => x.Id >= 3)
            .OrderBy(x => x.Id)
            .SeekAfter(3);

        var spec = left.Or(right).Take(10).Select(x => x.Id);
        Assert.Equal(new[] { 4, 5, 6 }, Items.AsQueryable().BySpecification(spec).ToArray());
        Assert.Equal(new[] { 4, 5, 6 }, Items.AsQueryable().BySpecification(right.And(
            Specification.Create<Item>().Where(x => x.Id <= 6))).Select(x => x.Id).ToArray());
    }

    [Fact]
    public void SeekAfterIsDeferredAndProducesQueryableWhereBeforeOrderBy()
    {
        var source = Items.AsQueryable();
        var spec = Specification.Create<Item>().OrderBy(x => x.Id).SeekAfter(3);
        var query = source.BySpecification(spec);
        var order = Assert.IsAssignableFrom<System.Linq.Expressions.MethodCallExpression>(query.Expression);
        Assert.Equal(nameof(Queryable.OrderBy), order.Method.Name);
        var where = Assert.IsAssignableFrom<System.Linq.Expressions.MethodCallExpression>(order.Arguments[0]);
        Assert.Equal(nameof(Queryable.Where), where.Method.Name);
    }

    [Fact]
    public void RejectsMissingOrderingIncorrectCursorShapeAndUnsupportedKeyTypes()
    {
        Assert.Throws<InvalidOperationException>(() => Specification.Create<Item>().SeekAfter(1));
        var ordering = Specification.Create<Item>().OrderBy(x => x.Id).ThenBy(x => x.Rank);
        Assert.Throws<ArgumentException>(() => ordering.SeekAfter(1));
        Assert.Throws<ArgumentException>(() => ordering.SeekAfter((1L, 2)));
        Assert.Throws<ArgumentException>(() =>
            Specification.Create<Item>().OrderBy(x => x.Id).SeekAfter(1L));

        Assert.Throws<NotSupportedException>(() =>
            Specification.Create<Item>().OrderBy(x => x.Name).SeekAfter("A"));
        Assert.Throws<NotSupportedException>(() =>
            Specification.Create<Item>().OrderBy(x => x.Optional).SeekAfter((int?)1));
        Assert.Throws<NotSupportedException>(() =>
            Specification.Create<Item>().OrderBy(x => (object)x.Id).SeekAfter((object)1));
    }

    [Fact]
    public void RejectsSkipAdditionalOrderingAndRepeatedCursorWithoutMutatingOriginal()
    {
        var ordering = Specification.Create<Item>().OrderBy(x => x.Id);
        var seek = ordering.SeekAfter(2);
        Assert.Throws<InvalidOperationException>(() => seek.Skip(1));
        Assert.Throws<InvalidOperationException>(() => seek.OrderBy(x => x.Rank));
        Assert.Throws<InvalidOperationException>(() => seek.OrderByDesc(x => x.Rank));
        Assert.Throws<InvalidOperationException>(() => seek.SeekAfter(3));
        Assert.Throws<InvalidOperationException>(() => ordering.Skip(1).SeekAfter(2));
        Assert.Throws<InvalidOperationException>(() => seek.And(Specification.Create<Item>().Skip(1)));
        Assert.Throws<InvalidOperationException>(() => Specification.Create<Item>().Skip(1).Or(seek));
        Assert.Throws<InvalidOperationException>(() => seek.And(ordering.SeekAfter(3)));

        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 },
            Items.AsQueryable().BySpecification(ordering).Select(x => x.Id).ToArray());
        Assert.Equal(new[] { 3, 4, 5, 6 },
            Items.AsQueryable().BySpecification(seek).Select(x => x.Id).ToArray());
    }
}
