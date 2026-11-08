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
        var entry = Assert.Single(spec.OrderingSteps());
        Assert.Equal(OrderDirectionEnum.Ascending, entry.Direction);
        Assert.Same(selector, entry.Expressions[0]);
        Assert.Equal(typeof(TKey), entry.Expressions[0].ReturnType);
        Assert.IsAssignableFrom<Expression<Func<OrderingItem, TKey>>>(entry.Expressions[0]);

        var query = new[] { new OrderingItem() }.AsQueryable().BySpecification(spec);
        var call = Assert.IsAssignableFrom<MethodCallExpression>(query.Expression);
        AssertCall(call, nameof(Queryable.OrderBy), typeof(TKey), entry.Expressions[0]);
    }

    [Fact]
    public void QueryExpressionUsesEachOriginalKeyTypeForThreeDifferentSorts()
    {
        var spec = Specification.Create<OrderingItem>()
            .OrderBy(i => i.Name)
            .ThenByDesc(i => i.Id)
            .ThenBy(i => i.Rank);
        var diagnostics = spec.OrderingSteps();

        Assert.Equal(new[] { typeof(string), typeof(int), typeof(long) },
            diagnostics.Select(item => item.Expressions[0].ReturnType));
        Assert.Equal(new[]
        {
            OrderDirectionEnum.Ascending,
            OrderDirectionEnum.Descending,
            OrderDirectionEnum.Ascending
        }, diagnostics.Select(item => item.Direction));

        IQueryable<OrderingItem> source = new[] { new OrderingItem() }.AsQueryable();
        IQueryable<OrderingItem> query = source.BySpecification(spec);
        var third = Assert.IsAssignableFrom<MethodCallExpression>(query.Expression);
        AssertCall(third, nameof(Queryable.ThenBy), typeof(long), diagnostics[2].Expressions[0]);
        var second = Assert.IsAssignableFrom<MethodCallExpression>(third.Arguments[0]);
        AssertCall(second, nameof(Queryable.ThenByDescending), typeof(int), diagnostics[1].Expressions[0]);
        var first = Assert.IsAssignableFrom<MethodCallExpression>(second.Arguments[0]);
        AssertCall(first, nameof(Queryable.OrderBy), typeof(string), diagnostics[0].Expressions[0]);
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

        Assert.Same(typed, typedSpec.OrderingSteps()[0].Expressions[0]);
        Assert.Equal(typeof(int), typedSpec.OrderingSteps()[0].Expressions[0].ReturnType);
        Assert.Equal(ExpressionType.MemberAccess, typed.Body.NodeType);
        Assert.Same(boxed, boxedSpec.OrderingSteps()[0].Expressions[0]);
        Assert.Equal(typeof(object), boxedSpec.OrderingSteps()[0].Expressions[0].ReturnType);
        Assert.Equal(ExpressionType.Convert, boxedSpec.OrderingSteps()[0].Expressions[0].Body.NodeType);
        Assert.Equal(ExpressionType.Convert, castSpec.OrderingSteps()[0].Expressions[0].Body.NodeType);
    }

    [Fact]
    public void PublicDiagnosticSnapshotsAreReadOnlyAndCannotAffectExecution()
    {
        var spec = Specification.Create<Student>().OrderBy(s => s.Age);
        var snapshot = spec.OrderingSteps();
        var list = Assert.IsAssignableFrom<IList<QueryPipelineStep>>(snapshot);
        Assert.Throws<NotSupportedException>(() => list.Clear());

        Assert.Equal("OrderBy", Assert.Single(snapshot).Operation);
        Assert.Equal(new[] { 20, 22, 30 },
            TestsStudents().BySpecification(spec).Select(s => s.Age).ToArray());
        Assert.Single(spec.OrderingSteps());
        Assert.Null(spec.LastStep("Take")?.Count);
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

        Assert.Empty(root.OrderingSteps());
        Assert.Equal(typeof(int), byAge.OrderingSteps()[0].Expressions[0].ReturnType);
        Assert.Equal(new[] { typeof(string), typeof(long) },
            combined.OrderingSteps().Select(e => e.Expressions[0].ReturnType));
        Assert.Equal(new[] { typeof(string), typeof(long) },
            byName.OrderingSteps().Select(e => e.Expressions[0].ReturnType));
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

        var keys = spec.OrderingSteps();
        Assert.Equal(new[] { typeof(string), typeof(long), typeof(int?) },
            keys.Select(x => x.Expressions[0].ReturnType));
        Assert.Equal(new[]
        {
            OrderDirectionEnum.Ascending,
            OrderDirectionEnum.Descending,
            OrderDirectionEnum.Ascending
        }, keys.Select(x => x.Direction));
        Assert.Same(name, keys[0].Expressions[0]);
        Assert.Same(rank, keys[1].Expressions[0]);
        Assert.Same(optional, keys[2].Expressions[0]);
        Assert.All(keys, x => Assert.NotEqual(ExpressionType.Convert, x.Expressions[0].Body.NodeType));

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
    public void MixedDirectionThenByOrderingMatchesLinq()
    {
        var rows = new[]
        {
            new OrderingItem { Id = 3, Name = "A", Rank = 5 },
            new OrderingItem { Id = 1, Name = "B", Rank = 4 },
            new OrderingItem { Id = 2, Name = "A", Rank = 5 },
            new OrderingItem { Id = 4, Name = "A", Rank = 3 }
        }.AsQueryable();

        var specification = Specification.Create<OrderingItem>()
            .OrderBy(i => i.Name).ThenByDesc(i => i.Rank).ThenBy(i => i.Id);

        var expected = rows.OrderBy(i => i.Name).ThenByDescending(i => i.Rank)
            .ThenBy(i => i.Id).Select(i => i.Id).ToArray();
        var actual = rows.BySpecification(specification).Select(i => i.Id).ToArray();

        Assert.Equal(new[] { 2, 3, 4, 1 }, actual);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ExplicitSecondaryOrderingKeepsImmutableBranchesAndReadOnlyDiagnostics()
    {
        var first = Specification.Create<OrderingItem>().OrderBy(i => i.Name);
        var snapshot = first.OrderingSteps();
        var byId = first.ThenBy(i => i.Id);
        var byRank = first.ThenByDesc(i => i.Rank);

        Assert.Single(snapshot);
        Assert.Single(first.OrderingSteps());
        Assert.Equal(2, byId.OrderingSteps().Count);
        Assert.Equal(2, byRank.OrderingSteps().Count);
        Assert.Equal(typeof(int), byId.OrderingSteps()[1].Expressions[0].ReturnType);
        Assert.Equal(typeof(long), byRank.OrderingSteps()[1].Expressions[0].ReturnType);
        var list = Assert.IsAssignableFrom<IList<QueryPipelineStep>>(
            byId.OrderingSteps());
        Assert.Throws<NotSupportedException>(() => list.Clear());
        Assert.Null(byId.LastStep("Take"));
    }

    [Fact]
    public void ThenByRequiresPrimaryOrderingEvenWhenSpecificationIsConfigured()
    {
        var missing = Assert.Throws<InvalidOperationException>(() =>
            Specification.Create<OrderingItem>().ThenBy(i => i.Id));
        Assert.Contains("existing OrderBy or OrderByDesc", missing.Message);

        var withTake = Assert.Throws<InvalidOperationException>(() =>
            Specification.Create<OrderingItem>().Take(5).ThenByDesc(i => i.Rank));
        Assert.Contains("ThenBy", withTake.Message);

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
        Assert.Single(first.OrderingSteps());
        Assert.Single(seek.OrderingSteps());
    }

    [Fact]
    public void ThenByRemainsComposableWithAndOrAndRetainsOrderingConflicts()
    {
        var filter = Specification.Create<OrderingItem>().Where(i => i.Id > 0);
        var ordering = Specification.Create<OrderingItem>()
            .OrderBy(i => i.Name).ThenBy(i => i.Id);
        var and = filter.And(ordering).ThenByDesc(i => i.Rank);
        var or = filter.Or(ordering).ThenByDesc(i => i.Rank);

        Assert.Equal(3, and.OrderingSteps().Count);
        Assert.Equal(3, or.OrderingSteps().Count);
        Assert.Single(ordering.OrderingSteps().Skip(1));
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
