using DimonSmart.Specification.Tests.Common;

namespace DimonSmart.Specification.Tests;

public sealed class ReusableProjectionAndContractTests : TestsBase
{
    [Fact]
    public void InterfaceTypedSpecificationsCanBeAppliedToQueryablesAndEnumerables()
    {
        ISpecification<Student> adults = Specification.Create<Student>().Where(s => s.Age >= 22);
        ISpecification<Student, string> names = Specification.Create<Student>()
            .Where(s => s.Age >= 22).Select(s => s.Name);

        Assert.Equal(new[] { 22, 30 }, Students.BySpecification(adults).Select(s => s.Age));
        Assert.Equal(new[] { "Alex", "Alex" }, Students.BySpecification(names));
        Assert.Equal(2, Students.BySpecificationCriteria(adults).Count());
        IEnumerable<Student> enumerable = Students.ToArray();
        Assert.Equal(2, enumerable.BySpecification(adults).Count());
        Assert.Equal(2, enumerable.BySpecification(names).Count());
    }

    [Fact]
    public void WithProjectionOfAppendsEveryOperationAndPreservesOriginals()
    {
        var first = Specification.Create<Student>().OrderBy(s => s.Age).Take(2);
        var projection = Specification.Create<Student>().Select(s => s.Name)
            .Where(name => name.StartsWith("A"));
        var result = first.WithProjectionOf(projection);

        Assert.Equal(new[] { "Alex" }, Students.BySpecification(result));
        Assert.Equal(new[] { "Alex", "Alex" }, Students.BySpecificationCriteria(result));
        Assert.Equal(new[] { "OrderBy", "Take", "Select", "Where" },
            result.PipelineSteps.Select(step => step.Operation));
        Assert.Equal(new[] { 20, 22 }, Students.BySpecification(first).Select(s => s.Age));
        Assert.Equal(new[] { "Alex", "Alex" }, Students.BySpecification(projection));
    }

    [Fact]
    public void ProjectionCanHaveSourceStageFiltersAndRemainComposableAfterAppending()
    {
        var prefix = Specification.Create<Student>().Where(s => s.Age >= 20);
        var projection = Specification.Create<Student>().Where(s => s.Age < 30)
            .Select(s => s.Name).OrderBy(name => name);
        var appended = prefix.WithProjectionOf(projection).Take(1);

        Assert.Equal(new[] { "Alex" }, Students.BySpecification(appended));
        Assert.Equal(new[] { "Alex", "Sofia" },
            Students.BySpecificationCriteria(appended).OrderBy(x => x));
        Assert.Equal("Take", appended.PipelineSteps[^1].Operation);
    }

    [Fact]
    public void InterfaceDispatchAndProjectionValidateNulls()
    {
        var spec = Specification.Create<Student>();
        ISpecification<Student> contract = spec;
        Assert.Equal("query", Assert.Throws<ArgumentNullException>(
            () => contract.ApplyTo(null!)).ParamName);
        Assert.Equal("projection", Assert.Throws<ArgumentNullException>(
            () => spec.WithProjectionOf<string>(null!)).ParamName);
    }
}
