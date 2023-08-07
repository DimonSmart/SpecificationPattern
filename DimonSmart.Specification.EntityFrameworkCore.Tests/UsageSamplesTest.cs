using DimonSmart.Specification.Tests.Common;

namespace DimonSmart.Specification.EntityFrameworkCore.Tests;

public class UsageSamplesTest
{
    [Fact]
    public void SequentialWhere()
    {
        var specification = EFCoreSpecification<Student>.Create()
            .Where(s => s.Age < 21)
            .Where(s => s.Age > 16);
    }

    [Fact]
    public void SequentialInclude()
    {
        var specification = EFCoreSpecification<Student>.Create()
            .Include(s => s.School)
            .Include(s => s.School.MainBook)
            .Include(s => s.Books)
            .ThenInclude(b => b.Author)
            .ThenInclude(a => a.Name)
            .Include(s => s.Books)
            .Where(s => (s.Age & 1) == 1)
            .Include(s => s.Books);
    }

    [Fact]
    public void MixedWhereInclude()
    {
        var specification = EFCoreSpecification<Student>.Create()
            .Include(s => s.School)
            .Where(s => s.Age < 21)
            .Include(s => s.School.MainBook)
            .Where(s => s.Age > 16);
    }
}