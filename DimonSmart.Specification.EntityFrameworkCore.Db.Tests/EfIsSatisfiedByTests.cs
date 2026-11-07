using DimonSmart.Specification.Tests.Common;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

public sealed class EfIsSatisfiedByTests
{
    [Fact]
    public void EfSpecificationsCheckCriteriaWithoutRequiringDbContext()
    {
        IEFCoreSpecification<Student> spec = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age >= 22)
            .OrderBy(s => s.Age)
            .Take(0)
            .Include(s => s.Books)
            .AsNoTracking()
            .TagWith("ignored by IsSatisfiedBy");

        var alex = new Student { Name = "Alex", Age = 22 };
        var sofia = new Student { Name = "Sofia", Age = 20 };
        Assert.True(spec.IsSatisfiedBy(alex));
        Assert.False(spec.IsSatisfiedBy(sofia));
        Assert.Equal("candidate", Assert.Throws<ArgumentNullException>(
            () => spec.IsSatisfiedBy(null!)).ParamName);
    }
}
