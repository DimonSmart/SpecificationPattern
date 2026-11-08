
using DimonSmart.Specification.Tests.Common;
using FluentAssertions;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

[Collection("Database collection")]
public class DBTypedSpecificationTests : TestsBase
{
    private readonly SchoolContext _context;

    public DBTypedSpecificationTests(DatabaseFixture fixture) => _context = fixture.TestDBContext;

    [Fact]
    public void IncludeAndThenIncludeAreMergedWithConfiguredOrdering()
    {
        var first = EFCoreSpecification.Create<Student>()
            .Include(s => s.Books)
            .ThenInclude(b => b.Author)
            .Where(s => s.Age >= 20);

        var second = EFCoreSpecification.Create<Student>()
            .Include(s => s.School)
            .Where(s => s.Name == "Alex")
            .OrderBy(s => s.Age);

        EFCoreSpecification<Student> result = first.And(second);

        result.EFCoreSpecificationData.Includes.Should()
            .BeEquivalentTo(new[] { "Books.Author", "School" });
        var students = _context.BySpecification(result.AsNoTracking()).ToList();
        students.Select(s => s.Age).Should().Equal(22, 30);

        Assert.NotSame(Alex22, students[0]);
        Assert.NotSame(Alex30, students[1]);
        Assert.Equal("Elementary School", students[0].School.Name);
        Assert.Equal("High School", students[1].School.Name);
        Assert.Equal(3, students[0].Books.Count);
        Assert.Equal(4, students[1].Books.Count);
        Assert.All(students.SelectMany(s => s.Books), book =>
        {
            Assert.NotNull(book.Author);
            Assert.False(string.IsNullOrWhiteSpace(book.Author.Name));
        });
    }

    [Fact]
    public void NonConflictingConfiguredOptionsAreCombined()
    {
        var first = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age > 18)
            .OrderByDesc(s => s.Age)
            .AsNoTracking();

        var second = EFCoreSpecification.Create<Student>()
            .Take(2)
            .AsSplitQuery();

        var result = first.And(second);

        result.EFCoreSpecificationData.IsAsNoTracking.Should().BeTrue();
        result.EFCoreSpecificationData.IsAsSplitQuery.Should().BeTrue();
        _context.BySpecification(result).Select(s => s.Age).ToList()
            .Should().Equal(30, 22);
    }

    [Fact]
    public void ConflictingTrackingModesAreRejected()
    {
        var first = EFCoreSpecification.Create<Student>().AsNoTracking();
        var second = EFCoreSpecification.Create<Student>().AsNoTrackingWithIdentityResolution();

        Action act = () => first.Or(second);

        act.Should().Throw<InvalidOperationException>().WithMessage("*tracking*");
    }

    [Fact]
    public void ConflictingSplitModesAreRejected()
    {
        var first = EFCoreSpecification.Create<Student>().AsSplitQuery();
        var second = EFCoreSpecification.Create<Student>().AsSingleQuery();

        Action act = () => first.And(second);

        act.Should().Throw<InvalidOperationException>().WithMessage("*splitting*");
    }

    [Fact]
    public void IncludeDoesNotTurnComposableIntoConfigured()
    {
        EFCoreSpecification<Student> source = EFCoreSpecification.Create<Student>()
            .Include(s => s.School)
            .Where(s => s.Age > 18);

        var configured = source.Take(1);
        source.LastStep("Take")?.Count.Should().BeNull();
        configured.LastStep("Take")?.Count.Should().Be(1);
        source.EFCoreSpecificationData.Includes.Should().ContainSingle().Which.Should().Be("School");
    }
}
