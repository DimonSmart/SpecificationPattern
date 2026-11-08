using System.Linq.Expressions;
using DimonSmart.Specification.Tests.Common;

namespace DimonSmart.Specification.Tests;

public sealed class ConditionalQueryOperationsTests : TestsBase
{
    [Fact]
    public void EnabledOperationsMatchUnconditionalOrderingAndPaging()
    {
        var root = Specification.Create<Student>().Where(s => s.Age >= 20);
        var conditional = root
            .OrderBy(s => s.Name, true)
            .ThenByDesc(s => s.Age, true)
            .ThenBy(s => s.Id, true)
            .ThenByDesc(s => s.Id, true)
            .Skip(1, true)
            .Take(1, true);
        var ordinary = root
            .OrderBy(s => s.Name)
            .ThenByDesc(s => s.Age)
            .ThenBy(s => s.Id)
            .ThenByDesc(s => s.Id)
            .Skip(1)
            .Take(1);

        Assert.Equal(
            ordinary.OrderingSteps().Select(x => (x.Direction, x.Expressions[0].ReturnType)),
            conditional.OrderingSteps().Select(x => (x.Direction, x.Expressions[0].ReturnType)));
        Assert.Equal(ordinary.LastStep("Skip")?.Count, conditional.LastStep("Skip")?.Count);
        Assert.Equal(ordinary.LastStep("Take")?.Count, conditional.LastStep("Take")?.Count);
        Assert.Equal(new[] { 22 }, Students.BySpecification(conditional).Select(s => s.Age).ToArray());
        Assert.Equal(
            Students.BySpecification(ordinary).Select(s => s.Id).ToArray(),
            Students.BySpecification(conditional).Select(s => s.Id).ToArray());
        Assert.Empty(root.OrderingSteps());
    }

    [Fact]
    public void DisabledOperationsKeepDataAndReturnSameInstance()
    {
        var root = Specification.Create<Student>().Where(s => s.Age >= 20);
        Specification<Student> configured = root.OrderBy(s => s.Name, false);

        Assert.Same(root, configured);
        Assert.Same(root.WhereStep()?.Expressions[0], configured.WhereStep()?.Expressions[0]);
        Assert.Empty(configured.OrderingSteps());
        Assert.Null(configured.LastStep("Skip")?.Count);
        Assert.Null(configured.LastStep("Take")?.Count);

        var unchanged = configured
            .OrderBy(s => s.Age, false)
            .ThenByDesc(s => s.Age, false)
            .ThenBy(s => s.Id, false)
            .ThenByDesc(s => s.Id, false)
            .Skip(10, false)
            .Take(1, false);

        Assert.Same(configured, unchanged);
        Assert.Equal(new[] { 20, 22, 30 },
            Students.BySpecification(unchanged).Select(s => s.Age).OrderBy(age => age).ToArray());
        Assert.Empty(root.OrderingSteps());
    }

    [Fact]
    public void MixedConditionalOrderingKeepsOnlyEnabledKeysAndTheirTypes()
    {
        Expression<Func<Student, string>> name = s => s.Name;
        Expression<Func<Student, int>> age = s => s.Age;
        Expression<Func<Student, int>> id = s => s.Id;

        var spec = Specification.Create<Student>()
            .OrderBy(name, false)
            .OrderByDesc(age, true)
            .ThenBy(name, false)
            .ThenByDesc(id, true)
            .ThenBy(age, false)
            .ThenBy(name, true);

        var keys = spec.OrderingSteps();
        Assert.Equal(3, keys.Count);
        Assert.Equal(new[]
        {
            OrderDirectionEnum.Descending,
            OrderDirectionEnum.Descending,
            OrderDirectionEnum.Ascending
        }, keys.Select(key => key.Direction));
        Assert.Same(age, keys[0].Expressions[0]);
        Assert.Same(id, keys[1].Expressions[0]);
        Assert.Same(name, keys[2].Expressions[0]);
        Assert.Equal(new[] { 30, 22, 20 },
            Students.BySpecification(spec).Select(s => s.Age).ToArray());
    }

    [Fact]
    public void EnabledThenByRequiresAnActualPrimaryOrdering()
    {
        var noPrimary = Specification.Create<Student>().OrderBy(s => s.Name, false);
        Assert.Same(noPrimary, noPrimary.ThenBy(s => s.Id, false));
        Assert.Same(noPrimary, noPrimary.ThenByDesc(s => s.Id, false));

        Assert.Throws<InvalidOperationException>(() => noPrimary.ThenBy(s => s.Id, true));
        Assert.Throws<InvalidOperationException>(() => noPrimary.ThenByDesc(s => s.Id, true));

        var onlyPaged = Specification.Create<Student>().Take(2);
        Assert.Same(onlyPaged, onlyPaged.ThenBy(s => s.Id, false));
        Assert.Throws<InvalidOperationException>(() => onlyPaged.ThenBy(s => s.Id, true));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InvalidArgumentsAreRejectedEvenForDisabledOperations(bool condition)
    {
        var root = Specification.Create<Student>();

        Assert.Equal("expression", Assert.Throws<ArgumentNullException>(() =>
            root.OrderBy<int>(null!, condition)).ParamName);
        Assert.Equal("expression", Assert.Throws<ArgumentNullException>(() =>
            root.OrderByDesc<int>(null!, condition)).ParamName);
        Assert.Equal("expression", Assert.Throws<ArgumentNullException>(() =>
            root.ThenBy<int>(null!, condition)).ParamName);
        Assert.Equal("expression", Assert.Throws<ArgumentNullException>(() =>
            root.ThenByDesc<int>(null!, condition)).ParamName);
        Assert.Equal("count", Assert.Throws<ArgumentOutOfRangeException>(() =>
            root.Skip(-1, condition)).ParamName);
        Assert.Equal("count", Assert.Throws<ArgumentOutOfRangeException>(() =>
            root.Take(-1, condition)).ParamName);
    }

    [Fact]
    public void DisabledOperationsAfterSeekDoNotCreateConflicts()
    {
        var seek = Specification.Create<Student>().OrderBy(s => s.Age).SeekAfter(20);
        var unchanged = seek
            .OrderBy(s => s.Name, false)
            .ThenByDesc(s => s.Id, false)
            .ThenBy(s => s.Id, false)
            .ThenByDesc(s => s.Id, false)
            .Skip(1, false)
            .Take(2, false);

        Assert.Same(seek, unchanged);
        Assert.Equal(new[] { 22, 30 },
            Students.BySpecification(unchanged).Select(s => s.Age).ToArray());
        Assert.Throws<InvalidOperationException>(() => seek.Skip(1, true));
        Assert.Throws<InvalidOperationException>(() => seek.ThenBy(s => s.Id, true));
        Assert.Throws<InvalidOperationException>(() => seek.OrderBy(s => s.Id, true));
    }

    [Fact]
    public void SkippedConfigurationDoesNotConflictWithAndOr()
    {
        var active = Specification.Create<Student>()
            .Where(s => s.Age >= 20)
            .OrderBy(s => s.Age)
            .Skip(1)
            .Take(1);
        var skipped = Specification.Create<Student>()
            .OrderBy(s => s.Name, false)
            .Skip(100, false)
            .Take(0, false);

        Assert.Equal(new[] { 22 },
            Students.BySpecification(active.And(skipped)).Select(s => s.Age).ToArray());
        Assert.Equal(new[] { 22 },
            Students.BySpecification(active.Or(skipped)).Select(s => s.Age).ToArray());

        var seek = Specification.Create<Student>().OrderBy(s => s.Age).SeekAfter(20);
        Assert.Equal(new[] { 22, 30 },
            Students.BySpecification(seek.And(skipped)).Select(s => s.Age).ToArray());
        Assert.Equal(new[] { 22, 30 },
            Students.BySpecification(seek.Or(skipped)).Select(s => s.Age).ToArray());
    }
}
