using DimonSmart.Specification.Tests.Common;

namespace DimonSmart.Specification.Tests;

public sealed class EnumerableBySpecificationTests : TestsBase
{
    [Fact]
    public void ListArrayAndIEnumerableAcceptOrdinarySpecificationsDirectly()
    {
        var specification = Specification.Create<Student>()
            .Where(s => s.Age >= 22)
            .OrderByDesc(s => s.Age);

        List<Student> list = [Alex22, Sofia20, Alex30];
        Student[] array = [Alex22, Sofia20, Alex30];
        IEnumerable<Student> enumerable = list;

        Assert.Equal([30, 22], list.BySpecification(specification).Select(s => s.Age));
        Assert.Equal([30, 22], array.BySpecification(specification).Select(s => s.Age));
        Assert.Equal([30, 22], enumerable.BySpecification(specification).Select(s => s.Age));
    }

    [Fact]
    public void ProjectionOverEnumerablePreservesSortingPagingAndResultType()
    {
        IEnumerable<Student> source = new[] { Alex22, Sofia20, Alex30 };
        var specification = Specification.Create<Student>()
            .Where(s => s.Name == "Alex")
            .OrderByDesc(s => s.Age)
            .Take(1)
            .Select(s => new { s.Name, s.Age });

        var result = source.BySpecification(specification);
        Assert.Equal(new[] { ("Alex", 30) }, result.Select(s => (s.Name, s.Age)));
    }

    [Fact]
    public void IEnumerableOverloadDoesNotEnumerateDuringConstruction()
    {
        var enumerations = 0;
        IEnumerable<Student> GetStudents()
        {
            enumerations++;
            yield return Alex22;
            yield return Sofia20;
            yield return Alex30;
        }

        var result = GetStudents().BySpecification(Specification.Create<Student>()
            .Where(s => s.Age > 20)
            .OrderBy(s => s.Age)
            .Take(1));

        Assert.Equal(0, enumerations);
        Assert.Equal(new[] { 22 }, result.Select(s => s.Age).ToArray());
        Assert.Equal(1, enumerations);
    }

    [Fact]
    public void ProjectedEnumerableOverloadDoesNotRunSelectorBeforeEnumeration()
    {
        var enumerations = 0;
        var projections = 0;
        IEnumerable<Student> GetStudents()
        {
            enumerations++;
            yield return Alex22;
            yield return Sofia20;
        }

        string MapName(Student student)
        {
            projections++;
            return student.Name;
        }

        var specification = Specification.Create<Student>()
            .Where(s => s.Age >= 20)
            .Select(s => MapName(s));

        var result = GetStudents().BySpecification(specification);

        Assert.Equal(0, enumerations);
        Assert.Equal(0, projections);
        Assert.Equal(new[] { "Alex", "Sofia" }, result.ToArray());
        Assert.Equal(1, enumerations);
        Assert.Equal(2, projections);
    }

    [Fact]
    public void IQueryableStillSelectsExistingQueryableOverload()
    {
        IQueryable<Student> source = new[] { Alex22, Sofia20, Alex30 }.AsQueryable();
        var specification = Specification.Create<Student>()
            .Where(s => s.Age >= 22)
            .OrderBy(s => s.Age);
        var projection = specification.Select(s => s.Age);

        IQueryable<Student> result = source.BySpecification(specification);
        IQueryable<int> projectedResult = source.BySpecification(projection);

        Assert.Equal(new[] { 22, 30 }, result.Select(s => s.Age).ToArray());
        Assert.Equal(new[] { 22, 30 }, projectedResult.ToArray());
        Assert.Equal(typeof(IQueryable<Student>), result.GetType().GetInterfaces()
            .First(i => i == typeof(IQueryable<Student>)));
    }

    [Fact]
    public void RuntimeIQueryableBehindIEnumerableUsesOriginalQueryableProvider()
    {
        IQueryable<Student> source = new[] { Alex22, Sofia20, Alex30 }.AsQueryable();
        IEnumerable<Student> erased = source;
        var specification = Specification.Create<Student>().Where(s => s.Age > 20);
        var result = erased.BySpecification(specification);
        Assert.IsAssignableFrom<IQueryable<Student>>(result);
        Assert.Equal(new[] { 22, 30 }, result.Select(s => s.Age).ToArray());
    }

    [Fact]
    public void EnumerableOverloadsValidateNullInputs()
    {
        IEnumerable<Student> nullSource = null!;
        var specification = Specification.Create<Student>();
        var projected = specification.Select(s => s.Age);
        IEnumerable<Student> source = [Alex22];

        Assert.Throws<ArgumentNullException>(() => nullSource.BySpecification(specification));
        Assert.Throws<ArgumentNullException>(() => source.BySpecification(
            (Specification<Student, Composable>)null!));
        Assert.Throws<ArgumentNullException>(() => nullSource.BySpecification(projected));
        Assert.Throws<ArgumentNullException>(() => source.BySpecification(
            (ProjectedSpecification<Student, int>)null!));
    }

    [Fact]
    public void KeysetPaginationWorksWithEnumerableOverload()
    {
        List<Student> source = [Alex22, Sofia20, Alex30];
        var spec = Specification.Create<Student>()
            .OrderBy(s => s.Age)
            .SeekAfter(20)
            .Take(2);

        Assert.Equal(new[] { 22, 30 }, source.BySpecification(spec).Select(s => s.Age).ToArray());
    }
}
