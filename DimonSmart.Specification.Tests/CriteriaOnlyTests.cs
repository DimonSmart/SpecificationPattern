using System.Linq.Expressions;
using DimonSmart.Specification.Tests.Common;

namespace DimonSmart.Specification.Tests;

public sealed class CriteriaOnlyTests : TestsBase
{
    [Fact]
    public void WhereOnlyAndEmptySpecificationReturnExpectedSets()
    {
        var source = Students;
        var filter = Specification.Create<Student>().Where(s => s.Age >= 22);

        Assert.Equal(new[] { 22, 30 },
            source.BySpecificationCriteria(filter).Select(s => s.Age).ToArray());
        Assert.Same(source, source.BySpecificationCriteria(Specification.Create<Student>()));
        Assert.Equal(3, source.BySpecificationCriteria(Specification.Create<Student>()).Count());
    }

    [Fact]
    public void MultipleWhereClausesAndComposedAndOrAreApplied()
    {
        var age = Specification.Create<Student>()
            .Where(s => s.Age >= 20)
            .Where(s => s.Age <= 22)
            .OrderBy(s => s.Name);
        var name = Specification.Create<Student>().Where(s => s.Name == "Alex").Take(2);

        Assert.Equal(new[] { 20, 22 },
            Students.BySpecificationCriteria(age).Select(s => s.Age).OrderBy(x => x).ToArray());
        Assert.Equal(new[] { 22 },
            Students.BySpecificationCriteria(age.And(name)).Select(s => s.Age).ToArray());
        Assert.Equal(new[] { 20, 22, 30 },
            Students.BySpecificationCriteria(age.Or(name)).Select(s => s.Age).OrderBy(x => x).ToArray());
    }

    [Fact]
    public void SortingAndPagingIncludingTakeZeroAreNotApplied()
    {
        var specification = Specification.Create<Student>()
            .Where(s => s.Age >= 20)
            .OrderByDesc(s => s.Age)
            .ThenBy(s => s.Name)
            .ThenByDesc(s => s.Id)
            .Skip(2)
            .Take(0);

        var query = Students.BySpecificationCriteria(specification);
        Assert.Equal(nameof(Queryable.Where), Assert.IsAssignableFrom<MethodCallExpression>(query.Expression).Method.Name);
        Assert.DoesNotContain("OrderBy", query.Expression.ToString());
        Assert.DoesNotContain("Skip", query.Expression.ToString());
        Assert.DoesNotContain("Take", query.Expression.ToString());
        Assert.Equal(new[] { 22, 20, 30 }, query.Select(s => s.Age).ToArray());
        Assert.Empty(Students.BySpecification(specification));
    }

    [Fact]
    public void BothSingleAndCompositeSeekAfterBoundariesAreExcluded()
    {
        var single = Specification.Create<Student>()
            .Where(s => s.Age >= 20)
            .OrderBy(s => s.Age)
            .SeekAfter(22)
            .Take(1);
        var composite = Specification.Create<Student>()
            .Where(s => s.Age >= 20)
            .OrderBy(s => s.Age)
            .ThenBy(s => s.Id)
            .SeekAfter((22, 0));

        Assert.Equal(3, Students.BySpecificationCriteria(single).Count());
        Assert.Equal(3, Students.BySpecificationCriteria(composite).Count());
        Assert.Equal(new[] { 30 }, Students.BySpecification(single).Select(s => s.Age).ToArray());
        Assert.Equal(new[] { 30 }, Students.BySpecification(composite).Select(s => s.Age).ToArray());
        Assert.DoesNotContain("Seek", Students.BySpecificationCriteria(composite).Expression.ToString());
    }

    [Fact]
    public void EnumerableAndQueryableEvaluateTheSameCriteriaWithoutMaterializing()
    {
        var enumerations = 0;
        IEnumerable<Student> Generate()
        {
            enumerations++;
            yield return Alex22;
            yield return Sofia20;
            yield return Alex30;
        }

        var specification = Specification.Create<Student>()
            .Where(s => s.Name == "Alex")
            .OrderByDesc(s => s.Age)
            .Skip(1)
            .Take(1);
        IEnumerable<Student> enumerable = Generate();
        var result = enumerable.BySpecificationCriteria(specification);

        Assert.Equal(0, enumerations);
        Assert.Equal(new[] { 22, 30 }, result.Select(s => s.Age).ToArray());
        Assert.Equal(1, enumerations);
        Assert.Equal(
            Students.BySpecificationCriteria(specification).Select(s => s.Age),
            result.Select(s => s.Age));

        IEnumerable<Student> erasedQueryable = Students;
        Assert.IsAssignableFrom<IQueryable<Student>>(erasedQueryable.BySpecificationCriteria(specification));
    }

    [Fact]
    public void ExistingSourceOperationsArePreserved()
    {
        var source = Students.Where(s => s.Age >= 20).OrderBy(s => s.Age).Take(2);
        var specification = Specification.Create<Student>()
            .Where(s => s.Name == "Alex")
            .OrderByDesc(s => s.Age)
            .Take(0);

        var query = source.BySpecificationCriteria(specification);
        Assert.Equal(new[] { 22 }, query.Select(s => s.Age).ToArray());
        Assert.Contains("Take", query.Expression.ToString());
        Assert.Contains("OrderBy", query.Expression.ToString());
    }

    [Fact]
    public void CriteriaOnlyDoesNotMutateSpecificationOrAlterSubsequentFullEvaluation()
    {
        var specification = Specification.Create<Student>()
            .Where(s => s.Age >= 20)
            .OrderBy(s => s.Age)
            .Skip(1)
            .Take(1);
        var original = Students.BySpecification(specification).Select(s => s.Age).ToArray();

        Assert.Equal(3, Students.BySpecificationCriteria(specification).Count());
        Assert.Equal(original, Students.BySpecification(specification).Select(s => s.Age).ToArray());
        Assert.Equal(new[] { 22 }, original);
        Assert.Equal(1, specification.LastStep("Skip")?.Count);
        Assert.Equal(1, specification.LastStep("Take")?.Count);
        Assert.Single(specification.OrderingSteps());

        var other = Specification.Create<Student>().OrderByDesc(s => s.Name);
        Assert.Throws<InvalidOperationException>(() => specification.And(other));
        Assert.Throws<InvalidOperationException>(() => specification.Or(other));
        Assert.Throws<InvalidOperationException>(() => specification.EnsureCompatibleWith(other));
    }

    [Fact]
    public void NullArgumentsAreRejectedForBothOverloads()
    {
        IQueryable<Student> nullQuery = null!;
        IEnumerable<Student> nullEnumerable = null!;
        var specification = Specification.Create<Student>();

        Assert.Equal("query", Assert.Throws<ArgumentNullException>(
            () => nullQuery.BySpecificationCriteria(specification)).ParamName);
        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(
            () => Students.BySpecificationCriteria((Specification<Student>)null!)).ParamName);
        Assert.Equal("source", Assert.Throws<ArgumentNullException>(
            () => nullEnumerable.BySpecificationCriteria(specification)).ParamName);
        IEnumerable<Student> enumerable = new[] { Alex22 };
        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(
            () => enumerable.BySpecificationCriteria((Specification<Student>)null!)).ParamName);
    }
}
