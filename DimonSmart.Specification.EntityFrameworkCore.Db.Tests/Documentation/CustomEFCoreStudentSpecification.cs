using DimonSmart.Specification.EntityFrameworkCore;
using DimonSmart.Specification.Tests.Common;

namespace DocumentationExamples;

public sealed class AdultStudentsSpec : EFCoreSpecification<Student>
{
    public AdultStudentsSpec(int minAge)
        : base(EFCoreSpecification.Create<Student>()
            .Where(s => s.Age >= minAge)
            .Include(s => s.Books)
            .AsNoTracking())
    {
    }
}
