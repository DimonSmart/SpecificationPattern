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
        var call = Assert.IsType<MethodCallExpression>(query.Expression);
        AssertCall(call, nameof(Queryable.OrderBy), typeof(TKey), entry.expr);
    }

    [Fact]
    public void QueryExpressionUsesEachOriginalKeyTypeForThreeDifferentSorts()
    {
        var spec = Specification.Create<OrderingItem>()
            .OrderBy(i => i.Name)
            .OrderByDesc(i => i.Id)
            .OrderBy(i => i.Rank);
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
        var third = Assert.IsType<MethodCallExpression>(query.Expression);
        AssertCall(third, nameof(Queryable.ThenBy), typeof(long), diagnostics[2].expr);
        var second = Assert.IsType<MethodCallExpression>(third.Arguments[0]);
        AssertCall(second, nameof(Queryable.ThenByDescending), typeof(int), diagnostics[1].expr);
        var first = Assert.IsType<MethodCallExpression>(second.Arguments[0]);
        AssertCall(first, nameof(Queryable.OrderBy), typeof(string), diagnostics[0].expr);
        Assert.Same(source.Expression, first.Arguments[0]);
    }

    private static void AssertCall(MethodCallExpression call, string name, Type keyType, LambdaExpression selector)
    {
        Assert.Equal(name, call.Method.Name);
        Assert.Equal(typeof(Queryable), call.Method.DeclaringType);
        Assert.Equal(keyType, call.Method.GetGenericArguments()[1]);
        var quoted = Assert.IsType<UnaryExpression>(call.Arguments[1]);
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
            .OrderByDesc(i => i.Rank)
            .OrderBy(i => i.Optional)
            .OrderBy(i => i.Id);

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
        var byName = root.OrderBy(i => i.Name).OrderByDesc(i => i.Rank);
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
}
