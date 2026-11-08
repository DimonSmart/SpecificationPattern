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

        Assert.Null(root.SpecificationData.TakeQ);
        Assert.Empty(root.SpecificationData.OrderExpressions);
        var results = Students.BySpecification(spec).ToArray();
        Assert.Equal(2, results.Length);
        Assert.All(results, s => Assert.True(s.Age >= 20));
        Assert.Equal(results.Select(s => s.Name).OrderBy(x => x).ToArray(),
            results.Select(s => s.Name).ToArray());

        var other = root.Take(1);
        Assert.Equal(1, other.SpecificationData.TakeQ);
        Assert.Empty(other.SpecificationData.OrderExpressions);
        Assert.Equal(results.Select(s => s.Id).ToArray(),
            Students.BySpecification(spec).Select(s => s.Id).ToArray());
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

        Assert.Single(sort.SpecificationData.OrderExpressions);
        Assert.Single(anotherSort.SpecificationData.OrderExpressions);
        Assert.Equal(1, skip.SpecificationData.SkipQ);
        Assert.Equal(2, anotherSkip.SpecificationData.SkipQ);
    }

    [Fact]
    public void CompatibleSpecificationsAreReusableAndDoNotMutateOperands()
    {
        var left = Specification.Create<Student>().Where(s => s.Age >= 20).OrderBy(s => s.Age);
        var right = Specification.Create<Student>().Where(s => s.Name == "Alex").Take(2);
        left.EnsureCompatibleWith(right);
        var and = left.And(right);
        var or = left.Or(right);
        Assert.Null(left.SpecificationData.TakeQ);
        Assert.Empty(right.SpecificationData.OrderExpressions);
        Assert.Equal(2, and.SpecificationData.TakeQ);
        Assert.Equal(2, or.SpecificationData.TakeQ);
        Assert.Single(and.SpecificationData.OrderExpressions);
        Assert.Single(or.SpecificationData.OrderExpressions);
        Assert.Equal(new[] { 22, 30 },
            Students.BySpecification(and).Select(s => s.Age).ToArray());
        Assert.Equal(new[] { 20, 22 },
            Students.BySpecification(or).Select(s => s.Age).ToArray());
        Assert.Same(left, left.OrderBy(s => s.Name, false));
        Assert.Same(right, right.Take(7, false));
    }
}
