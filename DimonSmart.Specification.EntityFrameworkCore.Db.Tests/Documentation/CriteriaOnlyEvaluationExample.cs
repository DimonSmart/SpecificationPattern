using DimonSmart.Specification.EntityFrameworkCore;
using DimonSmart.Specification.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace DocumentationExamples;

public static class CriteriaOnlyEvaluationExample
{
    public static async Task<(int Total, List<Student> Page)> QueryAsync(
        DbContext db, CancellationToken cancellationToken = default)
    {
        var specification = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age >= 18)
            .OrderBy(s => s.Name)
            .Skip(20)
            .Take(20);

        var total = await db.Set<Student>()
            .BySpecificationCriteria(specification)
            .CountAsync(cancellationToken);

        var students = await db.Set<Student>()
            .BySpecification(specification)
            .ToListAsync(cancellationToken);

        return (total, students);
    }
}
