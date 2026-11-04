using DimonSmart.Specification.Tests.Common;

namespace DimonSmart.Specification.EntityFrameworkCore.Tests;

public class NamedQueryFilterTests
{
    [Fact]
    public void DefaultNamedAndAllExposeCompatibleDiagnostics()
    {
        var original = EFCoreSpecification.Create<Student>();
        var originalData = original.EFCoreSpecificationData;
        EFCoreSpecification<Student, Configured> named =
            original.IgnoreQueryFilters("SoftDeletionFilter", "AuditFilter");
        EFCoreSpecification<Student, Configured> all = named.IgnoreQueryFilters();

        Assert.False(originalData.IsIgnoreQueryFilters);
        Assert.Empty(originalData.IgnoredQueryFilterNames);
        Assert.False(named.EFCoreSpecificationData.IsIgnoreQueryFilters);
        Assert.Equal(new[] { "SoftDeletionFilter", "AuditFilter" },
            named.EFCoreSpecificationData.IgnoredQueryFilterNames);
        Assert.True(all.EFCoreSpecificationData.IsIgnoreQueryFilters);
        Assert.Empty(all.EFCoreSpecificationData.IgnoredQueryFilterNames);
        Assert.Empty(original.EFCoreSpecificationData.IgnoredQueryFilterNames);
    }

    [Fact]
    public void ConsecutiveCallsDeduplicateOrdinallyAndPreserveOrder()
    {
        var first = EFCoreSpecification.Create<Student>()
            .IgnoreQueryFilters("A", "B", "A");
        var before = first.EFCoreSpecificationData;
        EFCoreSpecification<Student, Configured> second =
            first.IgnoreQueryFilters("B", "a", " A ", "C", "C");

        Assert.Equal(new[] { "A", "B" }, before.IgnoredQueryFilterNames);
        Assert.Equal(new[] { "A", "B", "a", " A ", "C" },
            second.EFCoreSpecificationData.IgnoredQueryFilterNames);
        Assert.Equal(new[] { "A", "B" }, first.EFCoreSpecificationData.IgnoredQueryFilterNames);
    }

    [Fact]
    public void CallerOwnedParamsArrayIsNeverRetained()
    {
        var names = new[] { "B", "C" };
        var spec = EFCoreSpecification.Create<Student>().IgnoreQueryFilters("A", names);
        names[0] = "Changed";
        names[1] = "Other";
        Assert.Equal(new[] { "A", "B", "C" },
            spec.EFCoreSpecificationData.IgnoredQueryFilterNames);
    }

    [Fact]
    public void DiagnosticCollectionCannotBeModified()
    {
        var names = EFCoreSpecification.Create<Student>()
            .IgnoreQueryFilters("A").EFCoreSpecificationData.IgnoredQueryFilterNames;
        Assert.Equal(new[] { "A" }, names);
        if (names is IList<string> list)
        {
            Assert.True(list.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => list[0] = "Changed");
        }
    }

    [Fact]
    public void InvalidFirstNameIsRejectedWithCorrectParameter()
    {
        var spec = EFCoreSpecification.Create<Student>();
        Assert.Equal("filterName", Assert.Throws<ArgumentNullException>(
            () => spec.IgnoreQueryFilters(null!)).ParamName);
        Assert.Equal("filterName", Assert.Throws<ArgumentException>(
            () => spec.IgnoreQueryFilters("")).ParamName);
        Assert.Equal("filterName", Assert.Throws<ArgumentException>(
            () => spec.IgnoreQueryFilters(" \t ")).ParamName);
    }

    [Fact]
    public void InvalidAdditionalNamesAreRejectedWithCorrectParameter()
    {
        var spec = EFCoreSpecification.Create<Student>();
        Assert.Equal("additionalFilterNames", Assert.Throws<ArgumentNullException>(
            () => spec.IgnoreQueryFilters("A", (string[])null!)).ParamName);
        Assert.Equal("additionalFilterNames", Assert.Throws<ArgumentException>(
            () => spec.IgnoreQueryFilters("A", "B", "")).ParamName);
        Assert.Equal("additionalFilterNames", Assert.Throws<ArgumentException>(
            () => spec.IgnoreQueryFilters("A", new string[] { null! })).ParamName);
        Assert.Equal("additionalFilterNames", Assert.Throws<ArgumentException>(
            () => spec.IgnoreQueryFilters("A", " \t ")).ParamName);
    }

    [Fact]
    public void AllDominatesNamedCallsButDoesNotSkipValidation()
    {
        var all = EFCoreSpecification.Create<Student>().IgnoreQueryFilters();
        var namedFirst = EFCoreSpecification.Create<Student>()
            .IgnoreQueryFilters("A").IgnoreQueryFilters();

        Assert.True(all.IgnoreQueryFilters("B").EFCoreSpecificationData.IsIgnoreQueryFilters);
        Assert.Empty(all.IgnoreQueryFilters("B").EFCoreSpecificationData.IgnoredQueryFilterNames);
        Assert.True(namedFirst.EFCoreSpecificationData.IsIgnoreQueryFilters);
        Assert.Empty(namedFirst.EFCoreSpecificationData.IgnoredQueryFilterNames);
        Assert.Equal("filterName", Assert.Throws<ArgumentException>(
            () => all.IgnoreQueryFilters(" ")).ParamName);
        Assert.Equal("additionalFilterNames", Assert.Throws<ArgumentException>(
            () => all.IgnoreQueryFilters("A", " ")).ParamName);
        Assert.Equal("additionalFilterNames", Assert.Throws<ArgumentNullException>(
            () => all.IgnoreQueryFilters("A", (string[])null!)).ParamName);
    }

    [Fact]
    public void AndAndOrMergeNamedFiltersInLeftToRightOrder()
    {
        var left = EFCoreSpecification.Create<Student>().IgnoreQueryFilters("A", "B");
        var right = EFCoreSpecification.Create<Student>().IgnoreQueryFilters("B", "C");

        Assert.Equal(new[] { "A", "B", "C" },
            left.And(right).EFCoreSpecificationData.IgnoredQueryFilterNames);
        Assert.Equal(new[] { "A", "B", "C" },
            left.Or(right).EFCoreSpecificationData.IgnoredQueryFilterNames);
        Assert.Equal(new[] { "B", "C", "A" },
            right.Or(left).EFCoreSpecificationData.IgnoredQueryFilterNames);
        Assert.Equal(new[] { "A", "B" }, left.EFCoreSpecificationData.IgnoredQueryFilterNames);
        Assert.Equal(new[] { "B", "C" }, right.EFCoreSpecificationData.IgnoredQueryFilterNames);
    }

    [Fact]
    public void DefaultIsNeutralAndAllDominatesBothCompositionDirections()
    {
        var defaults = EFCoreSpecification.Create<Student>();
        var named = defaults.IgnoreQueryFilters("A");
        var all = defaults.IgnoreQueryFilters();

        Assert.Empty(defaults.And(defaults).EFCoreSpecificationData.IgnoredQueryFilterNames);
        Assert.Equal(new[] { "A" }, defaults.And(named).EFCoreSpecificationData.IgnoredQueryFilterNames);
        Assert.Equal(new[] { "A" }, named.Or(defaults).EFCoreSpecificationData.IgnoredQueryFilterNames);

        Assert.True(all.And(named).EFCoreSpecificationData.IsIgnoreQueryFilters);
        Assert.True(named.And(all).EFCoreSpecificationData.IsIgnoreQueryFilters);
        Assert.True(all.Or(defaults).EFCoreSpecificationData.IsIgnoreQueryFilters);
        Assert.True(defaults.Or(all).EFCoreSpecificationData.IsIgnoreQueryFilters);
        Assert.True(all.Or(all).EFCoreSpecificationData.IsIgnoreQueryFilters);
        Assert.Empty(named.And(all).EFCoreSpecificationData.IgnoredQueryFilterNames);
        Assert.Empty(all.Or(named).EFCoreSpecificationData.IgnoredQueryFilterNames);
    }

    [Fact]
    public void NamedFiltersComposeWithOtherOptionsWithoutChangingTheirRules()
    {
        var left = EFCoreSpecification.Create<Student>()
            .IgnoreQueryFilters("A").AsNoTracking().TagWith("left");
        var right = EFCoreSpecification.Create<Student>()
            .IgnoreQueryFilters("B").TagWith("right");

        var combined = left.And(right);
        Assert.Equal(new[] { "A", "B" },
            combined.EFCoreSpecificationData.IgnoredQueryFilterNames);
        Assert.True(combined.EFCoreSpecificationData.IsAsNoTracking);
        Assert.Equal(string.Join(Environment.NewLine, "left", "right"),
            combined.EFCoreSpecificationData.Tag);

        Assert.Throws<InvalidOperationException>(() => left.Or(
            EFCoreSpecification.Create<Student>().AsNoTrackingWithIdentityResolution()));
    }
}
