
using DimonSmart.Specification.Tests.Common;
using FluentAssertions;

namespace DimonSmart.Specification.Tests;

public class TypedSpecificationTests : TestsBase
{
    [Fact]
    public void ComposableSpecificationsComposeAndRemainComposable()
    {
        Specification<Student> result = Specification.Create<Student>()
            .Where(s => s.Age > 20)
            .And(Specification.Create<Student>().Where(s => s.Name == "Alex"));

        Students.BySpecification(result).Select(s => s.Age).ToList()
            .Should().BeEquivalentTo(new[] { 22, 30 });
    }

    [Fact]
    public void ConfiguredSpecificationsWithDistinctOptionsCompose()
    {
        var ordering = Specification.Create<Student>().OrderByDesc(s => s.Age);
        var limit = Specification.Create<Student>().Take(2);

        Specification<Student> result = ordering.And(limit);

        Students.BySpecification(result).Select(s => s.Age).ToList()
            .Should().Equal(30, 22);
    }

    [Fact]
    public void ConflictingSortingIsRejectedWithoutChangingEitherOperand()
    {
        var left = Specification.Create<Student>().OrderBy(s => s.Age);
        var right = Specification.Create<Student>().OrderBy(s => s.Name);

        Action act = () => left.And(right);

        act.Should().Throw<InvalidOperationException>().WithMessage("*ordering*");
        left.SpecificationData.OrderExpressions.Should().HaveCount(1);
        right.SpecificationData.OrderExpressions.Should().HaveCount(1);
    }

    [Fact]
    public void ConflictingPagingIsRejected()
    {
        var left = Specification.Create<Student>().Take(10);
        var right = Specification.Create<Student>().Take(20);

        Action act = () => left.Or(right);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Take*");
    }

    [Fact]
    public void QueryConfigurationDoesNotMutateOriginalSpecification()
    {
        var original = Specification.Create<Student>().Where(s => s.Age > 18);
        var configured = original.OrderBy(s => s.Name).Take(1);

        original.SpecificationData.OrderExpressions.Should().BeEmpty();
        original.SpecificationData.TakeQ.Should().BeNull();
        configured.SpecificationData.OrderExpressions.Should().HaveCount(1);
        configured.SpecificationData.TakeQ.Should().Be(1);
    }

    [Fact]
    public void OrCombinesFiltersWithoutLosingConfiguredOptions()
    {
        var left = Specification.Create<Student>().Where(s => s.Age < 21);
        var right = Specification.Create<Student>().Where(s => s.Age == 30).OrderBy(s => s.Age);

        var result = left.Or(right);

        Students.BySpecification(result).Select(s => s.Age).ToList()
            .Should().Equal(20, 30);
    }
}
