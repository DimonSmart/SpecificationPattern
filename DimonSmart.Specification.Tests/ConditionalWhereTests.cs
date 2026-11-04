using System.Linq.Expressions;
using DimonSmart.Specification.Tests.Common;

namespace DimonSmart.Specification.Tests;

public class ConditionalWhereTests : TestsBase
{
    [Fact]
    public void TrueUsesTheOriginalPredicateAndMatchesOrdinaryWhere()
    {
        Expression<Func<Student, bool>> predicate = s => s.Age >= 22;
        var conditional = Specification.Create<Student>().Where(predicate, true);
        var ordinary = Specification.Create<Student>().Where(predicate);

        Assert.Same(predicate, conditional.GetWhereExpression());
        Assert.Equal(
            Students.BySpecification(ordinary).Select(s => s.Age).ToArray(),
            Students.BySpecification(conditional).Select(s => s.Age).ToArray());
    }

    [Fact]
    public void FalseIsAnIdentityOperationEvenForAnExistingFilter()
    {
        var source = Specification.Create<Student>().Where(s => s.Age >= 22);
        var originalPredicate = source.GetWhereExpression();

        var result = source.Where(s => s.Name == "Nobody", false);

        Assert.Same(source, result);
        Assert.Same(originalPredicate, result.GetWhereExpression());
        Assert.Equal(new[] { 22, 30 },
            Students.BySpecification(result).Select(s => s.Age).ToArray());

        var empty = Specification.Create<Student>();
        Assert.Same(empty, empty.Where(s => s.Age > 0, false));
        Assert.Null(empty.GetWhereExpression());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NullPredicateIsRejectedRegardlessOfCondition(bool condition)
    {
        var specification = Specification.Create<Student>();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            specification.Where(null!, condition));

        Assert.Equal("expression", exception.ParamName);
    }

    [Fact]
    public void MixedConditionalFiltersUseAndAlsoWithoutMutatingEarlierBranches()
    {
        var source = Specification.Create<Student>().Where(s => s.Age >= 20);
        var skipped = source.Where(s => s.Name == "Sofia", false);
        var filtered = skipped
            .Where(s => s.Age < 30, true)
            .Where(s => s.Name == "Alex", true)
            .Where(s => s.Age == 0, false);

        Assert.Same(source, skipped);
        Assert.Equal(ExpressionType.AndAlso, filtered.GetWhereExpression()!.Body.NodeType);
        Assert.Equal(new[] { 22 },
            Students.BySpecification(filtered).Select(s => s.Age).ToArray());
        Assert.Equal(new[] { 20, 22, 30 },
            Students.BySpecification(source).Select(s => s.Age).OrderBy(a => a).ToArray());
    }

    [Fact]
    public void BothTypeStatesArePreservedAndConfiguredFalseReturnsSameInstance()
    {
        Specification<Student, Composable> composable = Specification.Create<Student>()
            .Where(s => s.Age >= 20, true);
        Specification<Student, Configured> configured = composable
            .OrderBy(s => s.Age)
            .Where(s => s.Name == "Nobody", false);

        Assert.Same(configured, configured.Where(s => s.Age < 30, false));
        Specification<Student, Configured> filtered = configured.Where(s => s.Age < 30, true);

        Assert.Equal(new[] { 20, 22, 30 },
            Students.BySpecification(configured).Select(s => s.Age).ToArray());
        Assert.Equal(new[] { 20, 22 },
            Students.BySpecification(filtered).Select(s => s.Age).ToArray());
    }

    [Fact]
    public void AndAndOrTreatSkippedPredicatesAsAbsent()
    {
        var absent = Specification.Create<Student>().Where(s => s.Age < 0, false);
        var alex = Specification.Create<Student>().Where(s => s.Name == "Alex", true);
        var older = Specification.Create<Student>().Where(s => s.Age > 22);

        Assert.Null(absent.GetWhereExpression());
        Assert.Equal(new[] { 22, 30 },
            Students.BySpecification(absent.Or(alex)).Select(s => s.Age).OrderBy(a => a).ToArray());
        Assert.Equal(new[] { 22, 30 },
            Students.BySpecification(absent.And(alex)).Select(s => s.Age).OrderBy(a => a).ToArray());
        Assert.Equal(new[] { 22, 30 },
            Students.BySpecification(older.Or(alex)).Select(s => s.Age).OrderBy(a => a).ToArray());
    }

    [Fact]
    public void ConditionalWherePreservesNestedLambdaParameters()
    {
        var specification = Specification.Create<Student>()
            .Where(s => s.Age > 20)
            .Where(student => student.Books.Any(book => book.Name == "The Great Gatsby"), true);

        Assert.Equal(new[] { 22 },
            Students.BySpecification(specification).Select(s => s.Age).ToArray());
    }

    [Fact]
    public void ConditionalWhereWorksWithPagingProjectionAndSeekAfter()
    {
        var projected = Specification.Create<Student>()
            .Where(s => s.Age >= 20, true)
            .Where(s => s.Name == "Nobody", false)
            .OrderByDesc(s => s.Age)
            .Skip(1)
            .Take(1)
            .Select(s => s.Age);

        Assert.Equal(new[] { 22 }, Students.BySpecification(projected).ToArray());

        var seek = Specification.Create<Student>()
            .OrderBy(s => s.Age)
            .SeekAfter(20)
            .Where(s => s.Name == "Nobody", false)
            .Take(1);

        Assert.Equal(new[] { 22 },
            Students.BySpecification(seek).Select(s => s.Age).ToArray());
    }
}
