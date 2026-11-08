using DimonSmart.Specification.Tests.Common;

namespace DimonSmart.Specification.Tests;

public sealed class IsSatisfiedByTests : TestsBase
{
    [Fact]
    public void CombinesMultipleWhereClausesAndIgnoresOrderingAndPaging()
    {
        ISpecification<Student> specification = Specification.Create<Student>()
            .Where(s => s.Age >= 22)
            .Where(s => s.Name == "Alex")
            .OrderBy(s => s.Age)
            .SeekAfter(25)
            .Take(0);

        Assert.True(specification.IsSatisfiedBy(Alex22));
        Assert.True(specification.IsSatisfiedBy(Alex30));
        Assert.False(specification.IsSatisfiedBy(Sofia20));
        Assert.Empty(Students.BySpecification((Specification<Student>)specification));
        Assert.Equal(new[] { 22, 30 },
            Students.BySpecificationCriteria((Specification<Student>)specification).Select(s => s.Age));
    }

    [Fact]
    public void EmptyAndComposedSpecificationsUseCriteriaOnlySemantics()
    {
        var unrestricted = Specification.Create<Student>();
        Assert.True(unrestricted.IsSatisfiedBy(Sofia20));
        var age = Specification.Create<Student>().Where(s => s.Age >= 22);
        var name = Specification.Create<Student>().Where(s => s.Name == "Sofia");
        Assert.True(age.Or(name).IsSatisfiedBy(Sofia20));
        Assert.False(age.And(name).IsSatisfiedBy(Sofia20));
        Assert.True(age.Or(unrestricted).IsSatisfiedBy(Sofia20));
    }

    [Fact]
    public void LateWhereStillWorksWithoutCanonicalLegacyDiagnostics()
    {
        var spec = Specification.Create<Student>().Take(0).Where(s => s.Age >= 22);
        Assert.True(spec.IsSatisfiedBy(Alex22));
        Assert.False(spec.IsSatisfiedBy(Sofia20));
        Assert.Equal(new[] { "Take", "Where" }, spec.PipelineSteps.Select(step => step.Operation));
    }

    private sealed class PredicateProbe
    {
        public int CallCount { get; private set; }
        public bool IsOlderThanTwenty(Student student)
        {
            CallCount++;
            return student.Age > 20;
        }
    }

    [Fact]
    public void MultiplePredicatesShortCircuitAndCacheAcrossCalls()
    {
        var probe = new PredicateProbe();
        var spec = Specification.Create<Student>()
            .Where(s => s.Age > 20)
            .Where(s => probe.IsOlderThanTwenty(s))
            .Take(0)
            .Where(s => s.Name == "Alex");

        Assert.False(spec.IsSatisfiedBy(Sofia20));
        Assert.Equal(0, probe.CallCount);

        for (var i = 0; i < 5; i++)
            Assert.True(spec.IsSatisfiedBy(Alex22));

        Assert.Equal(5, probe.CallCount);
        Assert.Equal(2, Students.BySpecificationCriteria(spec).Count());
        Assert.Empty(Students.BySpecification(spec));
    }

    [Fact]
    public void CompiledPredicateIsThreadSafeAndImmutableBranchesRemainIndependent()
    {
        var original = Specification.Create<Student>().Where(s => s.Age >= 20);
        var branch = original.Where(s => s.Age >= 30);
        Parallel.For(0, 100, _ =>
        {
            Assert.True(original.IsSatisfiedBy(Alex22));
            Assert.False(branch.IsSatisfiedBy(Alex22));
            Assert.True(branch.IsSatisfiedBy(Alex30));
        });
    }

    [Fact]
    public void NullCandidateIsRejected()
    {
        ISpecification<Student> specification = Specification.Create<Student>();
        Assert.Equal("candidate", Assert.Throws<ArgumentNullException>(
            () => specification.IsSatisfiedBy(null!)).ParamName);
    }
}
