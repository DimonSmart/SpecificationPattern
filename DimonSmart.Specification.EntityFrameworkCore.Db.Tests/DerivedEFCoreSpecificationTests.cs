using DimonSmart.Specification.Tests.Common;
using DocumentationExamples;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

[Collection("Database collection")]
public sealed class DerivedEFCoreSpecificationTests(DatabaseFixture fixture)
{
    private readonly SchoolContext _context = fixture.TestDBContext;

    [Fact]
    public void DerivedSpecificationExecutesWithIncludesAndTrackingOptions()
    {
        var spec = new AdultStudentsSpec(22);

        var students = _context.BySpecification(spec)
            .OrderBy(s => s.Age)
            .ToList();

        Assert.Equal(new[] { 22, 30 }, students.Select(s => s.Age));
        Assert.All(students, student => Assert.NotEmpty(student.Books));
        Assert.Equal(new[] { "Books" }, spec.EFCoreSpecificationData.Includes);
        Assert.True(spec.EFCoreSpecificationData.IsAsNoTracking);
    }

    [Fact]
    public void DerivedSpecificationCanBeComposedWithoutMutatingItsSource()
    {
        EFCoreSpecification<Student> source = new AdultStudentsSpec(20);
        var narrowed = source.Where(s => s.Name == "Alex");
        var ordered = narrowed.And(EFCoreSpecification.Create<Student>()
            .OrderBy(s => s.Age));

        Assert.Equal(new[] { 20, 22, 30 },
            _context.BySpecification(source).OrderBy(s => s.Age).Select(s => s.Age).ToArray());
        Assert.Equal(new[] { 22, 30 },
            _context.BySpecification(ordered).Select(s => s.Age).ToArray());
        Assert.Equal(new[] { "Books" }, ordered.EFCoreSpecificationData.Includes);
        Assert.True(source.EFCoreSpecificationData.IsAsNoTracking);
    }

    [Fact]
    public void DerivedSpecificationStillDetectsConflictingOptions()
    {
        var spec = new AdultStudentsSpec(18);
        var tracked = EFCoreSpecification.Create<Student>().AsTracking();

        Assert.Throws<InvalidOperationException>(() => spec.And(tracked));
    }
}
