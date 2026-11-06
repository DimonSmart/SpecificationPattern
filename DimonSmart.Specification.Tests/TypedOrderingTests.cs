using System.Linq.Expressions;
using DimonSmart.Specification.Tests.Common;

namespace DimonSmart.Specification.Tests;

public sealed class TypedOrderingTests
{
    private enum Priority { Low, High }

    private sealed class NestedKey
    {
        public string Name { get; init; } = "";
    }

    private sealed class OrderingItem
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
        public long Rank { get; init; }
        public decimal Amount { get; init; }
        public DateTime Date { get; init; }
        public Guid Token { get; init; }
        public Priority Priority { get; init; }
        public int? Optional { get; init; }
        public NestedKey Nested { get; init; } = new();
    }

    [Fact]
    public void RetainsSelectorTypesWithoutLibraryAddedBoxing()
    {
        CheckSelector((OrderingItem item) => item.Id);
        CheckSelector((OrderingItem item) => item.Rank);
        CheckSelector((OrderingItem item) => item.Amount);
        CheckSelector((OrderingItem item) => item.Name);
        CheckSelector((OrderingItem item) => item.Date);
        CheckSelector((OrderingItem item) => item.Token);
        CheckSelector((OrderingItem item) => item.Priority);
        CheckSelector((OrderingItem item) => item.Optional);
        CheckSelector((OrderingItem item) => item.Nested.Name);
        CheckSelector((OrderingItem item) => item.Rank + 1L);
    }

    private static void CheckSelector<TKey>(Expression<Func<OrderingItem, TKey>> selector)
    {
        var spec = Specification.Create<OrderingItem>().OrderBy(selector);
        var entry = Assert.Single(spec.SpecificationData.OrderExpressions);
        Assert.Equal(OrderDirectionEnum.Ascending, entry.direction);
        Assert.Same(selector, entry.expr);
        Assert.Equal(typeof(TKey), entry.expr.ReturnType);
        Assert.IsAssignableFrom<Expression<Func<OrderingItem, TKey>>>(entry.expr);

        var query = new[] { new OrderingItem() }.AsQueryable().BySpecification(spec);
        var call = Assert.IsAssignableFrom<MethodCallExpression>(query.Expression);
        AssertCall(call, nameof(Queryable.OrderBy), typeof(TKey), entry.expr);
    }

    [Fact]
    public void QueryExpressionUsesEachOriginalKeyTypeForThreeDifferentSorts()
    {
        var spec = Specification.Create<OrderingItem>()
            .OrderBy(i => i.Name)
            .ThenByDesc(i => i.Id)
            .ThenBy(i => i.Rank);
        var diagnostics = spec.SpecificationData.OrderExpressions;

        Assert.Equal(new[] { typeof(string), typeof(int), typeof(long) },
            diagnostics.Select(item => item.expr.ReturnType));
        Assert.Equal(new[]
        {
            OrderDirectionEnum.Ascending,
            OrderDirectionEnum.Descending,
            OrderDirectionEnum.Ascending
        }, diagnostics.Select(item => item.direction));

        IQueryable<OrderingItem> source = new[] { new OrderingItem() }.AsQueryable();
        IQueryable<OrderingItem> query = source.BySpecification(spec);
        var third = Assert.IsAssignableFrom<MethodCallExpression>(query.Expression);
        AssertCall(third, nameof(Queryable.ThenBy), typeof(long), diagnostics[2].expr);
        var second = Assert.IsAssignableFrom<MethodCallExpression>(third.Arguments[0]);
        AssertCall(second, nameof(Queryable.ThenByDescending), typeof(int), diagnostics[1].expr);
        var first = Assert.IsAssignableFrom<MethodCallExpression>(second.Arguments[0]);
        AssertCall(first, nameof(Queryable.OrderBy), typeof(string), diagnostics[0].expr);
        Assert.Same(source.Expression, first.Arguments[0]);
    }

    private static void AssertCall(MethodCallExpression call, string name, Type keyType, LambdaExpression selector)
    {
        Assert.Equal(name, call.Method.Name);
        Assert.Equal(typeof(Queryable), call.Method.DeclaringType);
        Assert.Equal(keyType, call.Method.GetGenericArguments()[1]);
        var quoted = Assert.IsAssignableFrom<UnaryExpression>(call.Arguments[1]);
        Assert.Equal(ExpressionType.Quote, quoted.NodeType);
        Assert.Same(selector, quoted.Operand);
    }

    [Fact]
    public void MoreThanThreeLevelsRespectMixedDirectionsNullableKeysAndPaging()
    {
        var items = new[]
        {
            new OrderingItem { Id = 1, Name = "B", Rank = 2, Optional = 5 },
            new OrderingItem { Id = 2, Name = "A", Rank = 1, Optional = 4 },
            new OrderingItem { Id = 3, Name = "A", Rank = 1, Optional = null },
            new OrderingItem { Id = 4, Name = "A", Rank = 2, Optional = 9 },
            new OrderingItem { Id = 5, Name = "B", Rank = 1, Optional = 5 }
        }.AsQueryable();

        var spec = Specification.Create<OrderingItem>()
            .OrderBy(i => i.Name)
            .ThenByDesc(i => i.Rank)
            .ThenBy(i => i.Optional)
            .ThenBy(i => i.Id);

        Assert.Equal(new[] { 4, 3, 2, 1, 5 },
            items.BySpecification(spec).Select(i => i.Id).ToArray());
        Assert.Equal(new[] { 3, 2 }, items.BySpecification(spec)
            .Skip(1).Take(2).Select(i => i.Id).ToArray());
    }

    [Fact]
    public void ExplicitObjectSelectorsRemainUntouched()
    {
        Expression<Func<Student, int>> typed = s => s.Age;
        Expression<Func<Student, object>> boxed = s => s.Age;
        var typedSpec = Specification.Create<Student>().OrderBy(typed);
        var boxedSpec = Specification.Create<Student>().OrderBy(boxed);
        var castSpec = Specification.Create<Student>().OrderBy(s => (object)s.Age);

        Assert.Same(typed, typedSpec.SpecificationData.OrderExpressions[0].expr);
        Assert.Equal(typeof(int), typedSpec.SpecificationData.OrderExpressions[0].expr.ReturnType);
        Assert.Equal(ExpressionType.MemberAccess, typed.Body.NodeType);
        Assert.Same(boxed, boxedSpec.SpecificationData.OrderExpressions[0].expr);
        Assert.Equal(typeof(object), boxedSpec.SpecificationData.OrderExpressions[0].expr.ReturnType);
        Assert.Equal(ExpressionType.Convert, boxedSpec.SpecificationData.OrderExpressions[0].expr.Body.NodeType);
        Assert.Equal(ExpressionType.Convert, castSpec.SpecificationData.OrderExpressions[0].expr.Body.NodeType);
    }

    [Fact]
    public void PublicDiagnosticSnapshotsAreReadOnlyAndCannotAffectExecution()
    {
        var spec = Specification.Create<Student>().OrderBy(s => s.Age);
        var snapshot = spec.SpecificationData.OrderExpressions;
        var list = Assert.IsAssignableFrom<IList<(OrderDirectionEnum direction, LambdaExpression expr)>>(snapshot);
        Assert.Throws<NotSupportedException>(() => list.Clear());

        var dataCopy = Assert.IsType<SpecificationData<Student>>(spec.SpecificationData);
        dataCopy.WhereExpression = s => s.Age == 0;
        dataCopy.TakeQ = 0;
        Assert.Equal(new[] { 20, 22, 30 },
            TestsStudents().BySpecification(spec).Select(s => s.Age).ToArray());
        Assert.Single(spec.SpecificationData.OrderExpressions);
        Assert.Null(spec.SpecificationData.TakeQ);
    }

    private static IQueryable<Student> TestsStudents() => new[]
    {
        new Student { Age = 22 }, new Student { Age = 30 }, new Student { Age = 20 }
    }.AsQueryable();

    [Fact]
    public void IndependentBranchesAndCompositionPreserveAllKeyTypes()
    {
        var root = Specification.Create<OrderingItem>().Where(i => i.Id > 0);
        var byName = root.OrderBy(i => i.Name).ThenByDesc(i => i.Rank);
        var byAge = root.OrderBy(i => i.Id);
        var combined = root.And(byName);

        Assert.Empty(root.SpecificationData.OrderExpressions);
        Assert.Equal(typeof(int), byAge.SpecificationData.OrderExpressions[0].expr.ReturnType);
        Assert.Equal(new[] { typeof(string), typeof(long) },
            combined.SpecificationData.OrderExpressions.Select(e => e.expr.ReturnType));
        Assert.Equal(new[] { typeof(string), typeof(long) },
            byName.SpecificationData.OrderExpressions.Select(e => e.expr.ReturnType));
        Assert.Throws<InvalidOperationException>(() => byName.Or(byAge));

        var items = new[]
        {
            new OrderingItem { Id = 1, Name = "A", Rank = 1 },
            new OrderingItem { Id = 2, Name = "A", Rank = 2 },
            new OrderingItem { Id = 3, Name = "B", Rank = 3 }
        }.AsQueryable();
        Assert.Equal(new[] { 2, 1, 3 },
            items.BySpecification(combined).Select(i => i.Id).ToArray());
    }

    [Fact]
    public void RejectsNullSelectorsForBothGenericOrderingMethods()
    {
        Assert.Equal("expression", Assert.Throws<ArgumentNullException>(() =>
            Specification.Create<Student>().OrderBy<int>(null!)).ParamName);
        Assert.Equal("expression", Assert.Throws<ArgumentNullException>(() =>
            Specification.Create<Student>().OrderByDesc<int>(null!)).ParamName);
    }
    [Fact]
    public void ExplicitThenByPreservesOriginalTypedSelectorsAndQueryableMethodChain()
    {
        Expression<Func<OrderingItem, string>> name = item => item.Name;
        Expression<Func<OrderingItem, long>> rank = item => item.Rank;
        Expression<Func<OrderingItem, int?>> optional = item => item.Optional;
        var spec = Specification.Create<OrderingItem>()
            .OrderBy(name)
            .ThenByDesc(rank)
            .ThenBy(optional);

        var keys = spec.SpecificationData.OrderExpressions;
        Assert.Equal(new[] { typeof(string), typeof(long), typeof(int?) },
            keys.Select(x => x.expr.ReturnType));
        Assert.Equal(new[]
        {
            OrderDirectionEnum.Ascending,
            OrderDirectionEnum.Descending,
            OrderDirectionEnum.Ascending
        }, keys.Select(x => x.direction));
        Assert.Same(name, keys[0].expr);
        Assert.Same(rank, keys[1].expr);
        Assert.Same(optional, keys[2].expr);
        Assert.All(keys, x => Assert.NotEqual(ExpressionType.Convert, x.expr.Body.NodeType));

        var source = new[] { new OrderingItem() }.AsQueryable();
        var third = Assert.IsAssignableFrom<MethodCallExpression>(source.BySpecification(spec).Expression);
        AssertCall(third, nameof(Queryable.ThenBy), typeof(int?), optional);
        var second = Assert.IsAssignableFrom<MethodCallExpression>(third.Arguments[0]);
        AssertCall(second, nameof(Queryable.ThenByDescending), typeof(long), rank);
        var first = Assert.IsAssignableFrom<MethodCallExpression>(second.Arguments[0]);
        AssertCall(first, nameof(Queryable.OrderBy), typeof(string), name);
        Assert.Same(source.Expression, first.Arguments[0]);
    }

    [Fact]
    public void ExplicitThenByDescWorksAfterDescendingPrimaryAndWithThreeLevels()
    {
        var rows = new[]
        {
            new OrderingItem { Id = 1, Name = "A", Rank = 1 },
            new OrderingItem { Id = 2, Name = "B", Rank = 1 },
            new OrderingItem { Id = 3, Name = "B", Rank = 3 },
            new OrderingItem { Id = 4, Name = "A", Rank = 2 }
        }.AsQueryable();

        var spec = Specification.Create<OrderingItem>()
            .OrderByDesc(i => i.Name)
            .ThenByDesc(i => i.Rank)
            .ThenBy(i => i.Id);

        Assert.Equal(new[] { 3, 2, 4, 1 },
            rows.BySpecification(spec).Select(i => i.Id).ToArray());
        var third = Assert.IsAssignableFrom<MethodCallExpression>(rows.BySpecification(spec).Expression);
        Assert.Equal(nameof(Queryable.ThenBy), third.Method.Name);
        var second = Assert.IsAssignableFrom<MethodCallExpression>(third.Arguments[0]);
        Assert.Equal(nameof(Queryable.ThenByDescending), second.Method.Name);
        var first = Assert.IsAssignableFrom<MethodCallExpression>(second.Arguments[0]);
        Assert.Equal(nameof(Queryable.OrderByDescending), first.Method.Name);
    }

    [Fact]
    public void ExplicitLegacyAndMixedOrderingHaveIdenticalResults()
    {
        var rows = new[]
        {
            new OrderingItem { Id = 3, Name = "A", Rank = 5 },
            new OrderingItem { Id = 1, Name = "B", Rank = 4 },
            new OrderingItem { Id = 2, Name = "A", Rank = 5 },
            new OrderingItem { Id = 4, Name = "A", Rank = 3 }
        }.AsQueryable();
        var legacy = Specification.Create<OrderingItem>()
            .OrderBy(i => i.Name).ThenByDesc(i => i.Rank).ThenBy(i => i.Id);
        var explicitKeys = Specification.Create<OrderingItem>()
            .OrderBy(i => i.Name).ThenByDesc(i => i.Rank).ThenBy(i => i.Id);
        var mixed = Specification.Create<OrderingItem>()
            .OrderBy(i => i.Name).ThenByDesc(i => i.Rank).ThenBy(i => i.Id);

        var expected = rows.BySpecification(legacy).Select(i => i.Id).ToArray();
        Assert.Equal(new[] { 2, 3, 4, 1 }, expected);
        Assert.Equal(expected, rows.BySpecification(explicitKeys).Select(i => i.Id).ToArray());
        Assert.Equal(expected, rows.BySpecification(mixed).Select(i => i.Id).ToArray());
    }

    [Fact]
    public void ExplicitSecondaryOrderingKeepsImmutableBranchesAndReadOnlyDiagnostics()
    {
        var first = Specification.Create<OrderingItem>().OrderBy(i => i.Name);
        var snapshot = first.SpecificationData.OrderExpressions;
        var byId = first.ThenBy(i => i.Id);
        var byRank = first.ThenByDesc(i => i.Rank);

        Assert.Single(snapshot);
        Assert.Single(first.SpecificationData.OrderExpressions);
        Assert.Equal(2, byId.SpecificationData.OrderExpressions.Count);
        Assert.Equal(2, byRank.SpecificationData.OrderExpressions.Count);
        Assert.Equal(typeof(int), byId.SpecificationData.OrderExpressions[1].expr.ReturnType);
        Assert.Equal(typeof(long), byRank.SpecificationData.OrderExpressions[1].expr.ReturnType);
        var list = Assert.IsAssignableFrom<IList<(OrderDirectionEnum direction, LambdaExpression expr)>>(
            byId.SpecificationData.OrderExpressions);
        Assert.Throws<NotSupportedException>(() => list.Clear());
        var copy = Assert.IsType<SpecificationData<OrderingItem>>(byId.SpecificationData);
        copy.TakeQ = 0;
        Assert.Null(byId.SpecificationData.TakeQ);
    }

    [Fact]
    public void ThenByRequiresPrimaryOrderingEvenWhenSpecificationIsConfigured()
    {
        var missing = Assert.Throws<InvalidOperationException>(() =>
            Specification.Create<OrderingItem>().ThenBy(i => i.Id));
        Assert.Contains("existing OrderBy or OrderByDesc", missing.Message);

        var withTake = Assert.Throws<InvalidOperationException>(() =>
            Specification.Create<OrderingItem>().Take(5).ThenByDesc(i => i.Rank));
        Assert.Contains("ThenByDesc", withTake.Message);

        Assert.Throws<InvalidOperationException>(() =>
            Specification.Create<OrderingItem>().Skip(0).ThenBy(i => i.Id));
    }

    [Fact]
    public void ThenByRejectsNullSelectorsAndOrderingAfterSeekAfter()
    {
        var root = Specification.Create<OrderingItem>();
        Assert.Equal("expression", Assert.Throws<ArgumentNullException>(() =>
            root.ThenBy<int>(null!)).ParamName);
        Assert.Equal("expression", Assert.Throws<ArgumentNullException>(() =>
            root.OrderBy(i => i.Id).ThenByDesc<long>(null!)).ParamName);

        var first = root.OrderBy(i => i.Id);
        var seek = first.SeekAfter(2);
        Assert.Contains("SeekAfter", Assert.Throws<InvalidOperationException>(() =>
            seek.ThenBy(i => i.Rank)).Message);
        Assert.Throws<InvalidOperationException>(() => seek.ThenByDesc(i => i.Name));
        Assert.Single(first.SpecificationData.OrderExpressions);
        Assert.Single(seek.SpecificationData.OrderExpressions);
    }

    [Fact]
    public void ThenByRemainsComposableWithAndOrAndRetainsOrderingConflicts()
    {
        var filter = Specification.Create<OrderingItem>().Where(i => i.Id > 0);
        var ordering = Specification.Create<OrderingItem>()
            .OrderBy(i => i.Name).ThenBy(i => i.Id);
        var and = filter.And(ordering).ThenByDesc(i => i.Rank);
        var or = filter.Or(ordering).ThenByDesc(i => i.Rank);

        Assert.Equal(3, and.SpecificationData.OrderExpressions.Count);
        Assert.Equal(3, or.SpecificationData.OrderExpressions.Count);
        Assert.Single(ordering.SpecificationData.OrderExpressions.Skip(1));
        Assert.Throws<InvalidOperationException>(() =>
            ordering.And(Specification.Create<OrderingItem>().OrderBy(i => i.Rank)));
        Assert.Throws<InvalidOperationException>(() =>
            ordering.Or(Specification.Create<OrderingItem>().OrderBy(i => i.Id)));

        var rows = new[]
        {
            new OrderingItem { Id = 2, Name = "A", Rank = 1 },
            new OrderingItem { Id = 1, Name = "A", Rank = 2 },
            new OrderingItem { Id = 3, Name = "B", Rank = 3 }
        }.AsQueryable();
        Assert.Equal(new[] { 1, 2, 3 }, rows.BySpecification(and).Select(i => i.Id).ToArray());
        Assert.Equal(new[] { 1, 2, 3 }, rows.BySpecification(or).Select(i => i.Id).ToArray());
    }

    [Fact]
    public void ThenByIsAppliedBeforeTakeAndTerminalSelectDespiteCallOrder()
    {
        var rows = new[]
        {
            new OrderingItem { Id = 1, Name = "B", Rank = 1 },
            new OrderingItem { Id = 2, Name = "A", Rank = 2 },
            new OrderingItem { Id = 3, Name = "A", Rank = 3 }
        }.AsQueryable();
        var spec = Specification.Create<OrderingItem>()
            .Where(i => i.Id > 0, true)
            .OrderBy(i => i.Name)
            .ThenByDesc(i => i.Rank)
            .Take(2)
            .Select(i => i.Id);
        Assert.Equal(new[] { 3, 2 }, rows.BySpecification(spec).ToArray());
        Assert.Throws<InvalidOperationException>(() =>
            Specification.Create<OrderingItem>().OrderBy(i => i.Name).Take(2).ThenByDesc(i => i.Rank));
    }

    [Fact]
    public void ThenByParticipatesInFullSeekAfterCursor()
    {
        var rows = new[]
        {
            new OrderingItem { Id = 1, Rank = 3 },
            new OrderingItem { Id = 2, Rank = 2 },
            new OrderingItem { Id = 3, Rank = 2 },
            new OrderingItem { Id = 4, Rank = 1 }
        }.AsQueryable();
        var spec = Specification.Create<OrderingItem>()
            .OrderByDesc(i => i.Rank)
            .ThenBy(i => i.Id)
            .SeekAfter((2L, 2))
            .Take(2);

        Assert.Equal(new[] { 3, 4 }, rows.BySpecification(spec).Select(i => i.Id).ToArray());
        Assert.Throws<ArgumentException>(() => Specification.Create<OrderingItem>()
            .OrderByDesc(i => i.Rank).ThenBy(i => i.Id).SeekAfter(2L));
    }
}
