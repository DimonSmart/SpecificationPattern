using DimonSmart.Specification.EntityFrameworkCore;
using DimonSmart.Specification.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace DocumentationExamples;

public static class DynamicEFCoreSpecificationExample
{
    public static IQueryable<Student> Query(
        DbContext context, int? minAge, bool includeBooks, bool sortByName,
        int pageSize, bool noTracking)
    {
        var spec = EFCoreSpecification.Create<Student>();
        if (minAge.HasValue)
            spec = spec.Where(s => s.Age >= minAge.Value);
        if (includeBooks)
            spec = spec.Include(s => s.Books);
        if (sortByName)
            spec = spec.OrderBy(s => s.Name);
        if (pageSize > 0)
            spec = spec.Take(pageSize);
        if (noTracking)
            spec = spec.AsNoTracking();

        return context.BySpecification(spec);
    }
}
