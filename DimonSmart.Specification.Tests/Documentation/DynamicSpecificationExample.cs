using DimonSmart.Specification;
using StudentEntity = DimonSmart.Specification.Tests.Common.Student;

namespace DocumentationExamples;

public static class DynamicSpecificationExample
{
    public static IQueryable<StudentEntity> Query(
        IQueryable<StudentEntity> students, int? minAge, bool sortByName, int pageSize)
    {
        var spec = Specification.Create<StudentEntity>();
        if (minAge.HasValue)
            spec = spec.Where(s => s.Age >= minAge.Value);
        if (sortByName)
            spec = spec.OrderBy(s => s.Name);
        if (pageSize > 0)
            spec = spec.Take(pageSize);

        return students.BySpecification(spec);
    }
}
