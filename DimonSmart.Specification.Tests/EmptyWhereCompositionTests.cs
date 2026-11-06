using DimonSmart.Specification.Tests.Common;

namespace DimonSmart.Specification.Tests;

public sealed class EmptyWhereCompositionTests : TestsBase
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OrWithUnfilteredOperandMatchesAllEntities(bool unfilteredOnLeft)
    {
        var filtered = Specification.Create<Student>().Where(s => s.Age >= 22);
        var unfiltered = Specification.Create<Student>();
        var result = unfilteredOnLeft ? unfiltered.Or(filtered) : filtered.Or(unfiltered);

        Assert.Null(result.GetWhereExpression());
        Assert.Equal(Students.Count(), Students.BySpecification(result).Count());
        Assert.Equal(Students.Count(), Students.AsEnumerable().BySpecification(result).Count());
        Assert.NotNull(filtered.GetWhereExpression());
    }

    [Fact]
    public void OrWithBothUnfilteredOperandsMatchesAllEntities()
    {
        var result = Specification.Create<Student>().Or(Specification.Create<Student>());

        Assert.Null(result.GetWhereExpression());
        Assert.Equal(Students.Count(), Students.BySpecification(result).Count());
    }

    [Fact]
    public void DisabledConditionalWhereIsAlsoUnrestrictedForOr()
    {
        var filtered = Specification.Create<Student>().Where(s => s.Age >= 22);
        var skipped = Specification.Create<Student>().Where(s => s.Age < 20, false);

        Assert.Null(filtered.Or(skipped).GetWhereExpression());
        Assert.Equal(Students.Count(), Students.BySpecification(filtered.Or(skipped)).Count());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AndWithUnfilteredOperandKeepsTheOtherFilter(bool unfilteredOnLeft)
    {
        var filtered = Specification.Create<Student>().Where(s => s.Age >= 22);
        var unfiltered = Specification.Create<Student>();
        var result = unfilteredOnLeft ? unfiltered.And(filtered) : filtered.And(unfiltered);

        Assert.NotNull(result.GetWhereExpression());
        Assert.Equal(2, Students.BySpecification(result).Count());
    }

    [Fact]
    public void OrStillMergesConfigurationAndLaterWhereCanNarrowTheResult()
    {
        var filtered = Specification.Create<Student>().Where(s => s.Age >= 22);
        var unfilteredConfigured = Specification.Create<Student>().OrderBy(s => s.Age).Take(2);
        var combined = filtered.Or(unfilteredConfigured);

        Assert.Null(combined.GetWhereExpression());
        Assert.Equal(new[] { 20, 22 }, Students.BySpecification(combined).Select(s => s.Age).ToArray());

        var narrowed = combined.Where(s => s.Age >= 22);
        Assert.Equal(new[] { 22 }, Students.BySpecification(narrowed).Select(s => s.Age).ToArray());
        Assert.Throws<NotSupportedException>(() => narrowed.GetWhereExpression());
        Assert.Null(combined.GetWhereExpression());
        Assert.NotNull(filtered.GetWhereExpression());
    }

    [Fact]
    public void UnfilteredOrStillRejectsConflictingConfiguration()
    {
        var filtered = Specification.Create<Student>()
            .Where(s => s.Age >= 22).OrderBy(s => s.Age);
        var unfiltered = Specification.Create<Student>().OrderBy(s => s.Name);

        Assert.Throws<InvalidOperationException>(() => filtered.Or(unfiltered));
        Assert.Throws<InvalidOperationException>(() => unfiltered.Or(filtered));
    }
}
