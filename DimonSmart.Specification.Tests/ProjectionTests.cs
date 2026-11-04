using DimonSmart.Specification.Tests.Common;
using System.Linq.Expressions;

namespace DimonSmart.Specification.Tests;

public class ProjectionTests : TestsBase
{
    private sealed record StudentSummary(int Age, string Name);

    private sealed class SelectionCounter
    {
        public int Calls { get; private set; }

        public string Transform(string name)
        {
            Calls++;
            return name.ToUpperInvariant();
        }
    }

    [Fact]
    public void SupportsDtoScalarAnonymousAndEntityResults()
    {
        var dto = Students.BySpecification(Specification.Create<Student>()
            .Select(s => new StudentSummary(s.Age, s.Name))).ToList();
        Assert.Equal(3, dto.Count);
        Assert.Contains(new StudentSummary(22, "Alex"), dto);

        IQueryable<string> names = Students.BySpecification(
            Specification.Create<Student>().Select(s => s.Name));
        Assert.Equal(new[] { "Alex", "Sofia", "Alex" }, names.ToArray());

        IQueryable<int> ages = Students.BySpecification(
            Specification.Create<Student>().Select(s => s.Age));
        Assert.Equal(new[] { 22, 20, 30 }, ages.ToArray());

        var anonymous = Students.BySpecification(
            Specification.Create<Student>().Select(s => new { s.Name, s.Age })).ToArray();
        Assert.Equal(20, anonymous[1].Age);

        IQueryable<Student> entities = Students.BySpecification(
            Specification.Create<Student>().Select(s => s));
        Assert.Same(Alex22, entities.First());
    }

    [Fact]
    public void AppliesWhereAndAllOrderingBeforePagingAndSelect()
    {
        var specification = Specification.Create<Student>()
            .Where(s => s.Age > 18)
            .OrderBy(s => s.Name)
            .OrderByDesc(s => s.Age)
            .Skip(1)
            .Take(1)
            .Select(s => new StudentSummary(s.Age, s.Name));

        Assert.Equal(new[] { new StudentSummary(22, "Alex") },
            Students.BySpecification(specification).ToArray());
    }

    [Fact]
    public void SelectAfterComposableWhereAndTakeZeroAreSupported()
    {
        ProjectedSpecification<Student, string> names = Specification.Create<Student>()
            .Where(s => s.Age > 20)
            .Select(s => s.Name);
        Assert.Equal(new[] { "Alex", "Alex" }, Students.BySpecification(names).ToArray());

        var empty = Specification.Create<Student>()
            .OrderBy(s => s.Name).Take(0).Select(s => s.Name);
        Assert.Empty(Students.BySpecification(empty));
    }

    [Fact]
    public void ProjectionWorksAfterAndOrWithoutChangingCompositionRules()
    {
        var adults = Specification.Create<Student>().Where(s => s.Age > 20);
        var alex = Specification.Create<Student>().Where(s => s.Name == "Alex");
        var sortAndPage = Specification.Create<Student>().OrderByDesc(s => s.Age).Take(1);

        Assert.Equal(new[] { 30, 22 }, Students.BySpecification(
            adults.And(alex).OrderByDesc(s => s.Age).Select(s => s.Age)).ToArray());

        Assert.Equal(new[] { 30 }, Students.BySpecification(
            adults.Or(Specification.Create<Student>().Where(s => s.Age == 20))
                .And(sortAndPage).Select(s => s.Age)).ToArray());

        Assert.Throws<InvalidOperationException>(() =>
            Specification.Create<Student>().OrderBy(s => s.Age)
                .And(Specification.Create<Student>().OrderBy(s => s.Name)).Select(s => s.Name));
    }

    [Fact]
    public void SourceAndMultipleProjectionsRemainIndependent()
    {
        var source = Specification.Create<Student>().Where(s => s.Age > 18).OrderBy(s => s.Age);
        var names = source.Select(s => s.Name);
        var ages = source.Select(s => s.Age);

        var snapshot = source.SpecificationData.OrderExpressions;
        var mutable = Assert.IsAssignableFrom<IList<(OrderDirectionEnum direction, LambdaExpression expr)>>(snapshot);
        Assert.Throws<NotSupportedException>(() => mutable.Clear());
        Assert.Equal(new[] { "Sofia", "Alex", "Alex" }, Students.BySpecification(names).ToArray());
        Assert.Equal(new[] { 20, 22, 30 }, Students.BySpecification(ages).ToArray());
        Assert.Equal(new[] { 20, 22, 30 },
            Students.BySpecification(source).Select(s => s.Age).ToArray());
    }

    [Fact]
    public void RejectsNullSelectorAndNullExecutionArgumentsImmediately()
    {
        var selectorException = Assert.Throws<ArgumentNullException>(() =>
            Specification.Create<Student>().Select<string>(null!));
        Assert.Equal("selector", selectorException.ParamName);

        var projected = Specification.Create<Student>().Select(s => s.Name);
        Assert.Equal("query", Assert.Throws<ArgumentNullException>(() =>
            SpecificationExtension.BySpecification<Student, string>(null!, projected)).ParamName);
        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(() =>
            SpecificationExtension.BySpecification<Student, string>(Students, null!)).ParamName);
    }

    [Fact]
    public void ConstructionDoesNotEnumerateOrInvokeSelector()
    {
        var enumerations = 0;
        var tracker = new SelectionCounter();

        IEnumerable<Student> Source()
        {
            enumerations++;
            yield return Alex22;
            yield return Sofia20;
        }

        IQueryable<Student> source = Source().AsQueryable();
        Expression<Func<Student, string>> selector = s => tracker.Transform(s.Name);
        var projected = Specification.Create<Student>().Where(s => s.Age > 18).Select(selector);
        IQueryable<string> query = source.BySpecification(projected);
        Assert.Equal(0, enumerations);
        Assert.Equal(0, tracker.Calls);

        Assert.Equal(new[] { "ALEX", "SOFIA" }, query.ToArray());
        Assert.Equal(1, enumerations);
        Assert.Equal(2, tracker.Calls);
    }
}
