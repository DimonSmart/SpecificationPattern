using DimonSmart.Specification.Tests.Common;

namespace DimonSmart.Specification.EntityFrameworkCore.Tests;

public class UsageSamplesTests
{
    [Fact]
    public void RepeatedIncludesPreserveMostSpecificPathsAcrossWhere()
    {
        var root = EFCoreSpecification.Create<Student>()
            .Include(s => s.School);

        var specification = root
            .Include(s => s.School.MainBook)
            .Include(s => s.Books)
            .ThenInclude(b => b.Author)
            .Include(s => s.Books)
            .Where(s => (s.Age & 1) == 1)
            .Include(s => s.Books);

        Assert.Equal(new[] { "School" }, root.EFCoreSpecificationData.Includes);
        Assert.Equal(new[] { "School.MainBook", "Books.Author" },
            specification.EFCoreSpecificationData.Includes);
        Assert.Equal(new[] { "Where" },
            specification.PipelineSteps.Select(step => step.Operation));
        Assert.True(specification.IsSatisfiedBy(new Student { Age = 21 }));
        Assert.False(specification.IsSatisfiedBy(new Student { Age = 20 }));
    }

    [Fact]
    public void InterleavedWhereAndIncludesPreserveBothPredicatesAndLongestPath()
    {
        var specification = EFCoreSpecification.Create<Student>()
            .Include(s => s.School)
            .Where(s => s.Age < 21)
            .Include(s => s.School.MainBook)
            .Where(s => s.Age > 16);

        Assert.Equal(new[] { "School.MainBook" },
            specification.EFCoreSpecificationData.Includes);
        Assert.Equal(new[] { "Where", "Where" },
            specification.PipelineSteps.Select(step => step.Operation));
        Assert.False(specification.IsSatisfiedBy(new Student { Age = 16 }));
        Assert.True(specification.IsSatisfiedBy(new Student { Age = 17 }));
        Assert.True(specification.IsSatisfiedBy(new Student { Age = 20 }));
        Assert.False(specification.IsSatisfiedBy(new Student { Age = 21 }));
    }
}
