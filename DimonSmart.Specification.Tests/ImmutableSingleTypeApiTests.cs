using DimonSmart.Specification.Tests.Common;

namespace DimonSmart.Specification.Tests;

public sealed class ImmutableSingleTypeApiTests : TestsBase
{
    [Fact]
    public void DynamicConfigurationUsesOneStaticTypeAndKeepsIndependentBranches()
    {
        Specification<Student> root = Specification.Create<Student>().Where(s => s.Age >= 18);
        var spec = root;
        var filterByAge = true;
        var sortByName = true;
        var pageSize = 2;
        if (filterByAge)
            spec = spec.Where(s => s.Age >= 20);
        if (sortByName)
            spec = spec.OrderBy(s => s.Name);
        if (pageSize > 0)
            spec = spec.Take(pageSize);

        Assert.Null(root.LastStep("Take")?.Count);
        Assert.Empty(root.OrderingSteps());
        var results = Students.BySpecification(spec).ToArray();
        Assert.Equal(2, results.Length);
        Assert.All(results, s => Assert.True(s.Age >= 20));
        Assert.Equal(results.Select(s => s.Name).OrderBy(x => x).ToArray(),
            results.Select(s => s.Name).ToArray());

        var other = root.Take(1);
        Assert.Equal(1, other.LastStep("Take")?.Count);
        Assert.Empty(other.OrderingSteps());
        Assert.Equal(results.Select(s => s.Id).ToArray(),
            Students.BySpecification(spec).Select(s => s.Id).ToArray());
    }

    [Fact]
    public void CanonicalDiagnosticsAndCriteriaAreDerivedFromTheExecutablePipeline()
    {
        var specification = Specification.Create<Student>()
            .Where(s => s.Age >= 20)
            .OrderBy(s => s.Age)
            .ThenBy(s => s.Id)
            .Skip(1)
            .Take(1);

        Assert.Equal(new[] { "Where", "OrderBy", "ThenBy", "Skip", "Take" },
            specification.PipelineSteps.Select(step => step.Operation));
        Assert.NotNull(specification.WhereStep()?.Expressions[0]);
        Assert.Equal(2, specification.OrderingSteps().Count);
        Assert.Equal(1, specification.LastStep("Skip")?.Count);
        Assert.Equal(1, specification.LastStep("Take")?.Count);
        Assert.Equal(new[] { 22 },
            Students.BySpecification(specification).Select(s => s.Age).ToArray());
        Assert.True(specification.IsSatisfiedBy(Alex30));
        Assert.False(specification.IsSatisfiedBy(new Student { Age = 19 }));
    }

    [Fact]
    public void PipelineStepsExposeReadOnlyTypedOrderingAndPagingMetadata()
    {
        var specification = Specification.Create<Student>()
            .Where(s => s.Age >= 20)
            .OrderBy(s => s.Age)
            .ThenByDesc(s => s.Id)
            .Skip(1)
            .Take(0);

        var steps = specification.PipelineSteps;
        Assert.Equal(new[] { "Where", "OrderBy", "ThenByDesc", "Skip", "Take" },
            steps.Select(step => step.Operation));
        Assert.Equal(OrderDirectionEnum.Ascending, steps[1].Direction);
        Assert.Equal(OrderDirectionEnum.Descending, steps[2].Direction);
        Assert.Equal(typeof(int), steps[2].Expressions[0].ReturnType);
        Assert.Equal(1, steps[3].Count);
        Assert.Equal(0, steps[4].Count);
        Assert.Null(steps[0].Count);
        Assert.Throws<InvalidOperationException>(() => _ = steps[0].Direction);
        var mutable = Assert.IsAssignableFrom<IList<QueryPipelineStep>>(steps);
        Assert.Throws<NotSupportedException>(() => mutable.Clear());
        Assert.Empty(Students.BySpecification(specification));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CoreCompositionAndExplicitValidationHaveSameConflictRules(bool or)
    {
        var sort = Specification.Create<Student>().OrderBy(s => s.Name);
        var anotherSort = Specification.Create<Student>().OrderByDesc(s => s.Age);
        var skip = Specification.Create<Student>().Skip(1);
        var anotherSkip = Specification.Create<Student>().Skip(2);
        var take = Specification.Create<Student>().Take(1);
        var anotherTake = Specification.Create<Student>().Take(2);
        var seek = Specification.Create<Student>().OrderBy(s => s.Age).SeekAfter(20);
        var anotherSeek = Specification.Create<Student>().OrderBy(s => s.Age).SeekAfter(22);

        var conflicts = new (Specification<Student> left, Specification<Student> right, string reason)[]
        {
            (sort, anotherSort, "ordering"),
            (skip, anotherSkip, "Skip"),
            (take, anotherTake, "Take"),
            (seek, anotherSeek, "SeekAfter"),
            (seek, skip, "SeekAfter"),
            (skip, seek, "SeekAfter")
        };
        foreach (var (left, right, reason) in conflicts)
        {
            var explicitError = Assert.Throws<InvalidOperationException>(() => left.EnsureCompatibleWith(right));
            Assert.Contains(reason, explicitError.Message, StringComparison.OrdinalIgnoreCase);
            var compositionError = Assert.Throws<InvalidOperationException>(
                () => { _ = or ? left.Or(right) : left.And(right); });
            Assert.Contains(reason, compositionError.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(or ? "Or" : "And", compositionError.Message);
        }

        Assert.Single(sort.OrderingSteps());
        Assert.Single(anotherSort.OrderingSteps());
        Assert.Equal(1, skip.LastStep("Skip")?.Count);
        Assert.Equal(2, anotherSkip.LastStep("Skip")?.Count);
    }

    [Fact]
    public void CompatibleSpecificationsAreReusableAndDoNotMutateOperands()
    {
        var left = Specification.Create<Student>().Where(s => s.Age >= 20).OrderBy(s => s.Age);
        var right = Specification.Create<Student>().Where(s => s.Name == "Alex").Take(2);
        left.EnsureCompatibleWith(right);
        var and = left.And(right);
        var or = left.Or(right);
        Assert.Null(left.LastStep("Take")?.Count);
        Assert.Empty(right.OrderingSteps());
        Assert.Equal(2, and.LastStep("Take")?.Count);
        Assert.Equal(2, or.LastStep("Take")?.Count);
        Assert.Single(and.OrderingSteps());
        Assert.Single(or.OrderingSteps());
        Assert.Equal(new[] { 22, 30 },
            Students.BySpecification(and).Select(s => s.Age).ToArray());
        Assert.Equal(new[] { 20, 22 },
            Students.BySpecification(or).Select(s => s.Age).ToArray());
        Assert.Same(left, left.OrderBy(s => s.Name, false));
        Assert.Same(right, right.Take(7, false));
    }
}
