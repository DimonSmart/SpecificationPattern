using DimonSmart.Specification.Tests.Common;

namespace DimonSmart.Specification.EntityFrameworkCore.Tests;

public class UsageSamplesTests
{
    [Fact]
    public void SequentialWhere()
    {
        _ = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age < 21)
            .Where(s => s.Age > 16);
    }

    [Fact]
    public void SequentialInclude()
    {
        _ = EFCoreSpecification.Create<Student>()
            .Include(s => s.School)
            .Include(s => s.School.MainBook)
            .Include(s => s.Books)
            .ThenInclude(b => b.Author)
            .Include(s => s.Books)
            .Where(s => (s.Age & 1) == 1)
            .Include(s => s.Books);
    }

    [Fact]
    public void MixedWhereInclude()
    {
        _ = EFCoreSpecification.Create<Student>()
            .Include(s => s.School)
            .Where(s => s.Age < 21)
            .Include(s => s.School.MainBook)
            .Where(s => s.Age > 16);
    }

    [Fact]
    public void FilteredIncludeWithThenInclude()
    {
        _ = EFCoreSpecification.Create<Student>()
            .Include(s => s.Books.Where(b => b.Name.StartsWith("A"))
                .OrderBy(b => b.Name).ThenBy(b => b.Id).Skip(1).Take(2))
            .ThenInclude(b => b.Author);
    }
}
