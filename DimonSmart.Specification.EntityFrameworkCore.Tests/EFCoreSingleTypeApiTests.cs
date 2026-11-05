using DimonSmart.Specification.Tests.Common;

namespace DimonSmart.Specification.EntityFrameworkCore.Tests;

public sealed class EFCoreSingleTypeApiTests
{
    [Fact]
    public void DynamicOptionsPreserveStaticTypeAndTypedThenInclude()
    {
        EFCoreSpecification<Student> root = EFCoreSpecification.Create<Student>();
        var spec = root;
        var includeBooks = true;
        var noTracking = true;
        if (includeBooks)
            spec = spec.Include(s => s.Books);
        if (noTracking)
            spec = spec.AsNoTracking();
        spec = spec.Where(s => s.Age > 18).OrderBy(s => s.Name).Take(10);

        Assert.Empty(root.EFCoreSpecificationData.Includes);
        Assert.False(root.EFCoreSpecificationData.IsAsNoTracking);
        Assert.Equal(new[] { "Books" }, spec.EFCoreSpecificationData.Includes);
        Assert.True(spec.EFCoreSpecificationData.IsAsNoTracking);
        var typed = root.Include(s => s.Books).ThenInclude(b => b.Author);
        Assert.Equal(new[] { "Books.Author" }, typed.EFCoreSpecificationData.Includes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExplicitValidationAndCompositionRejectSameEfConflicts(bool or)
    {
        var tracking = EFCoreSpecification.Create<Student>().AsTracking();
        var noTracking = EFCoreSpecification.Create<Student>().AsNoTracking();
        var split = EFCoreSpecification.Create<Student>().AsSplitQuery();
        var single = EFCoreSpecification.Create<Student>().AsSingleQuery();
        var filtered = EFCoreSpecification.Create<Student>().Include(s => s.Books.Take(1));
        var unfiltered = EFCoreSpecification.Create<Student>().Include(s => s.Books);
        var conflicts = new (EFCoreSpecification<Student> left, EFCoreSpecification<Student> right, string reason)[]
        {
            (tracking, noTracking, "tracking"),
            (split, single, "splitting"),
            (filtered, unfiltered, "Books")
        };

        foreach (var (left, right, reason) in conflicts)
        {
            var checkError = Assert.Throws<InvalidOperationException>(() => left.EnsureCompatibleWith(right));
            Assert.Contains(reason, checkError.Message, StringComparison.OrdinalIgnoreCase);
            var compositionError = Assert.Throws<InvalidOperationException>(
                () => { _ = or ? left.Or(right) : left.And(right); });
            Assert.Contains(reason, compositionError.Message, StringComparison.OrdinalIgnoreCase);
        }
        Assert.False(tracking.EFCoreSpecificationData.IsAsNoTracking);
        Assert.Equal(new[] { "Books" }, filtered.EFCoreSpecificationData.Includes);
    }

    [Fact]
    public void CompatibleEfOptionsMergeAndSequentialSettingsAreLastWins()
    {
        var left = EFCoreSpecification.Create<Student>()
            .AsNoTracking().AsTracking().AsSplitQuery().AsSingleQuery().TagWith("first");
        var right = EFCoreSpecification.Create<Student>()
            .IgnoreQueryFilters("SoftDelete").TagWith("second");
        left.EnsureCompatibleWith(right);
        var combined = left.And(right);
        Assert.True(combined.EFCoreSpecificationData.IsAsTracking);
        Assert.True(combined.EFCoreSpecificationData.IsAsSingleQuery);
        Assert.Equal(string.Join(Environment.NewLine, "first", "second"),
            combined.EFCoreSpecificationData.Tag);
        Assert.Equal(new[] { "SoftDelete" }, combined.EFCoreSpecificationData.IgnoredQueryFilterNames);
        Assert.Equal("first", left.EFCoreSpecificationData.Tag);
    }
}
