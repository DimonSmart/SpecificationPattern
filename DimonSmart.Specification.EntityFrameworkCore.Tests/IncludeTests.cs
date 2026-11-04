using DimonSmart.Specification.Tests.Common;
using FluentAssertions;

namespace DimonSmart.Specification.EntityFrameworkCore.Tests;

public class IncludeTests
{
    [Fact]
    public void DirectReferenceChainAndPrefixDeduplication()
    {
        var spec = EFCoreSpecification.Create<Student>()
            .Include(s => s.School)
            .Include(s => s.School.MainBook)
            .Include(s => s.School.MainBook.Author);

        spec.EFCoreSpecificationData.Includes.Should()
            .Equal("School.MainBook.Author");

        var reverse = EFCoreSpecification.Create<Student>()
            .Include(s => s.School.MainBook.Author)
            .Include(s => s.School.MainBook)
            .Include(s => s.School);

        reverse.EFCoreSpecificationData.Includes.Should()
            .Equal("School.MainBook.Author");
    }

    [Fact]
    public void TypedThenIncludeKeepsElementTypeAndState()
    {
        EFCoreSpecification<Student, Composable> composable =
            EFCoreSpecification.Create<Student>()
                .Include(s => s.Books)
                .ThenInclude(b => b.Author);

        EFCoreSpecification<Student, Configured> configured =
            EFCoreSpecification.Create<Student>()
                .Take(3)
                .Include(s => s.Books.Where(b => b.Name.StartsWith("A")))
                .ThenInclude(b => b.Author);

        composable.EFCoreSpecificationData.Includes.Should().Equal("Books.Author");
        configured.EFCoreSpecificationData.Includes.Should().Equal("Books.Author");
    }

    [Fact]
    public void DistinctFiltersOnSameNavigationConflictDuringAndOrAndInclude()
    {
        var a = EFCoreSpecification.Create<Student>().Include(s => s.Books.Take(2));
        var b = EFCoreSpecification.Create<Student>().Include(s => s.Books.Take(2));

        Action and = () => a.And(b);
        Action or = () => a.Or(b);
        Action builder = () => a.Include(s => s.Books.Where(b => b.Name.StartsWith("A")));

        and.Should().Throw<InvalidOperationException>().WithMessage("*Books*different filtered includes*");
        or.Should().Throw<InvalidOperationException>().WithMessage("*Books*different filtered includes*");
        builder.Should().Throw<InvalidOperationException>().WithMessage("*Books*different filtered includes*");

        a.EFCoreSpecificationData.Includes.Should().Equal("Books");
        b.EFCoreSpecificationData.Includes.Should().Equal("Books");
    }

    [Fact]
    public void FilteredAndUnfilteredIncludeConflictImmediately()
    {
        var unfiltered = EFCoreSpecification.Create<Student>().Include(s => s.Books);
        var filtered = EFCoreSpecification.Create<Student>().Include(s => s.Books.Take(1));

        Action builder = () => unfiltered.Include(s => s.Books.Take(2));
        Action composition = () => filtered.Or(unfiltered);

        builder.Should().Throw<InvalidOperationException>().WithMessage("*Books*unfiltered vs filtered*");
        composition.Should().Throw<InvalidOperationException>().WithMessage("*Books*unfiltered vs filtered*");
    }

    [Fact]
    public void BranchesFromSameFilteredSourceShareFilterIdentity()
    {
        var source = EFCoreSpecification.Create<Student>()
            .Include(s => s.Books.Where(b => b.Name.StartsWith("A")));
        var withAuthor = source.ThenInclude(b => b.Author);
        var combined = withAuthor.And(source.Specification);

        combined.EFCoreSpecificationData.Includes.Should().Equal("Books.Author");
        source.EFCoreSpecificationData.Includes.Should().Equal("Books");
        withAuthor.EFCoreSpecificationData.Includes.Should().Equal("Books.Author");
    }

    [Fact]
    public void FilterConflictOnSharedIntermediateSegmentDetected()
    {
        var filtered = EFCoreSpecification.Create<Student>()
            .Include(s => s.Books.Take(1)).ThenInclude(b => b.Author);
        var unfiltered = EFCoreSpecification.Create<Student>()
            .Include(s => s.Books).ThenInclude(b => b.Author);

        Action combine = () => filtered.And(unfiltered);
        combine.Should().Throw<InvalidOperationException>().WithMessage("*Books*unfiltered vs filtered*");
    }

    [Fact]
    public void UnsupportedOperatorFailsBeforeQueryExecution()
    {
        Action invalid = () => EFCoreSpecification.Create<Student>()
            .Include(s => s.Books.Select(b => b.Name));
        invalid.Should().Throw<NotSupportedException>().WithMessage("*Select*");
    }

    [Fact]
    public void QueryModesAreLastWinsAndConflictingCompositionThrows()
    {
        var configured = EFCoreSpecification.Create<Student>()
            .AsNoTracking().AsNoTrackingWithIdentityResolution()
            .AsSingleQuery().AsSplitQuery();
        configured.EFCoreSpecificationData.IsAsNoTrackingWithIdentityResolution.Should().BeTrue();
        configured.EFCoreSpecificationData.IsAsNoTracking.Should().BeFalse();
        configured.EFCoreSpecificationData.IsAsSplitQuery.Should().BeTrue();
        configured.EFCoreSpecificationData.IsAsSingleQuery.Should().BeFalse();

        Action tracking = () => EFCoreSpecification.Create<Student>()
            .AsNoTracking().And(EFCoreSpecification.Create<Student>().AsNoTrackingWithIdentityResolution());
        Action splitting = () => EFCoreSpecification.Create<Student>()
            .AsSplitQuery().Or(EFCoreSpecification.Create<Student>().AsSingleQuery());

        tracking.Should().Throw<InvalidOperationException>().WithMessage("*tracking*");
        splitting.Should().Throw<InvalidOperationException>().WithMessage("*splitting*");
    }

    [Fact]
    public void OptionsMergeAndTagsAreImmutableAndOrdered()
    {
        var source = EFCoreSpecification.Create<Student>().TagWith("A");
        var left = source.TagWith("B").IgnoreAutoIncludes();
        var right = EFCoreSpecification.Create<Student>().TagWith("A").IgnoreQueryFilters();
        var merged = left.And(right);

        merged.EFCoreSpecificationData.Tag.Should().Be(
            string.Join(Environment.NewLine, "A", "B", "A"));
        merged.EFCoreSpecificationData.IsIgnoreAutoIncludes.Should().BeTrue();
        merged.EFCoreSpecificationData.IsIgnoreQueryFilters.Should().BeTrue();
        source.EFCoreSpecificationData.Tag.Should().Be("A");

        var blank = source.TagWith("  ").TagWith("");
        blank.EFCoreSpecificationData.Tag.Should().Be("A");
        Action nullTag = () => source.TagWith(null!);
        nullTag.Should().Throw<ArgumentNullException>();
    }
}
