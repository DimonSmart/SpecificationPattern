using DimonSmart.Specification.Tests.Common;

namespace DimonSmart.Specification.EntityFrameworkCore.Tests;

public class UsageSamplesTests
{
    [Fact]
    public void SequentialWhere()
    {
        _ = EFCoreSpecification<Student>.Create()
            .Where(s => s.Age < 21)
            .Where(s => s.Age > 16);
    }

    [Fact]
    public void SequentialInclude()
    {
        _ = EFCoreSpecification<Student>.Create()
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
        _ = EFCoreSpecification<Student>.Create()
            .Include(s => s.School)
            .Where(s => s.Age < 21)
            .Include(s => s.School.MainBook)
            .Where(s => s.Age > 16);
    }
}