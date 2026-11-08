using DimonSmart.Specification.Tests.Common;

namespace DimonSmart.Specification.Tests;

public sealed class ComposablePipelineTests
{
    private static Student[] StudentsData() =>
    [
        new Student { Id = 1, Age = 18, Name = "A", Books =
        [
            new Book { Id = 11, Name = "Alpha" },
            new Book { Id = 12, Name = "Beta" }
        ] },
        new Student { Id = 2, Age = 25, Name = "B", Books =
        [
            new Book { Id = 21, Name = "Atlas" },
            new Book { Id = 22, Name = "Amber" }
        ] }
    ];

    [Fact]
    public void TakeBeforeSelectManyAndTakeAfterSelectManyAreNotEquivalent()
    {
        var source = StudentsData().AsQueryable();
        var before = Specification.Create<Student>()
            .OrderBy(s => s.Id).Take(1).SelectMany(s => s.Books);
        var after = Specification.Create<Student>()
            .SelectMany(s => s.Books).OrderBy(b => b.Id).Take(1);

        Assert.Equal(new[] { 11, 12 }, source.BySpecification(before).Select(b => b.Id));
        Assert.Equal(new[] { 11 }, source.BySpecification(after).Select(b => b.Id));
        Assert.Equal(new[] { "OrderBy", "Take", "SelectMany" },
            before.PipelineSteps.Select(s => s.Operation));
    }

    [Fact]
    public void ProjectionMayContinueThroughDtoScalarAndAnotherProjection()
    {
        var source = StudentsData();
        var spec = Specification.Create<Student>()
            .Select(s => new { s.Id, s.Name })
            .Where(x => x.Id > 0)
            .Select(x => x.Name)
            .Where(name => name.StartsWith("A"))
            .Select(name => name.Length)
            .OrderBy(length => length);

        Assert.Equal(new[] { 1 }, source.AsEnumerable().BySpecification(spec));
        Assert.Equal(new[] { 1, 1, 2, 2, 3, 3 },
            spec.PipelineSteps.Select(s => s.StageId));
        Assert.Equal(typeof(int), spec.PipelineSteps[^1].OutputType);
    }

    [Fact]
    public void SelectManyResultSelectorCanBeFollowedByAnotherProjection()
    {
        var spec = Specification.Create<Student>()
            .SelectMany(s => s.Books, (s, b) => new { StudentId = s.Id, Book = b.Name })
            .Where(x => x.Book.StartsWith("A"))
            .Select(x => x.StudentId)
            .OrderBy(id => id);

        Assert.Equal(new[] { 1, 2, 2 },
            StudentsData().AsQueryable().BySpecification(spec));
        Assert.Equal(2, spec.PipelineSteps[0].Expressions.Count);
    }

    [Fact]
    public void CriteriaOnlyPreservesLateFiltersAndSelectManyButOmitsLimits()
    {
        var source = StudentsData().AsQueryable();
        var spec = Specification.Create<Student>()
            .OrderBy(s => s.Id)
            .Take(1)
            .SelectMany(s => s.Books)
            .Where(b => b.Name.StartsWith("A"))
            .Take(1);

        Assert.Equal(new[] { 11 }, source.BySpecification(spec).Select(b => b.Id));
        Assert.Equal(new[] { 11, 21, 22 },
            source.BySpecificationCriteria(spec).Select(b => b.Id).OrderBy(x => x));
        Assert.Equal(new[] { 11, 21, 22 },
            StudentsData().AsEnumerable().BySpecificationCriteria(spec).Select(b => b.Id).OrderBy(x => x));
        Assert.Single(source.Take(1).BySpecificationCriteria(spec));
    }

    [Fact]
    public void RepeatedOrderByStartsANewBlockAndThenByRequiresContiguity()
    {
        var repeated = Specification.Create<Student>()
            .OrderBy(s => s.Age).OrderBy(s => s.Id);
        var query = StudentsData().AsQueryable().BySpecification(repeated);
        Assert.Equal(new[] { "OrderBy", "OrderBy" },
            repeated.PipelineSteps.Select(s => s.Operation));
        Assert.Equal("OrderBy", ((System.Linq.Expressions.MethodCallExpression)query.Expression).Method.Name);
        Assert.Throws<InvalidOperationException>(() =>
            repeated.EnsureCompatibleWith(Specification.Create<Student>()));

        Assert.Throws<InvalidOperationException>(() =>
            Specification.Create<Student>().OrderBy(s => s.Id)
                .Where(s => s.Id > 0).ThenBy(s => s.Age));
        Assert.Throws<InvalidOperationException>(() =>
            Specification.Create<Student>().OrderBy(s => s.Id)
                .Take(1).ThenBy(s => s.Age));
    }

    [Fact]
    public void SeekAfterUsesOrderingOfProjectedStageAndResetsAfterAnotherSelect()
    {
        var root = Specification.Create<Student>().OrderBy(s => s.Id).Skip(1);
        var selected = root.Select(s => new { s.Id, s.Age });
        var spec = selected.OrderBy(s => s.Id).SeekAfter(1).Take(2);
        Assert.Equal(new[] { 2 }, StudentsData().AsQueryable().BySpecification(spec).Select(s => s.Id));
        Assert.Contains(spec.PipelineSteps, step => step.Operation == "SeekAfter" &&
            step.StageId == 1 && step.IsKeysetOrderingBlock);
        Assert.Throws<InvalidOperationException>(() => selected.OrderBy(s => s.Id).Take(1).SeekAfter(1));
        Assert.Throws<InvalidOperationException>(() => spec.Skip(1));
        Assert.Throws<InvalidOperationException>(() => spec.OrderBy(s => s.Id));
    }

    [Fact]
    public void SourceLateWhereIsExecutableButNotCanonicallyComposable()
    {
        var spec = Specification.Create<Student>().Take(1).Where(s => s.Age > 20);
        Assert.Empty(StudentsData().AsQueryable().BySpecification(spec));
        Assert.Equal(new[] { "Take", "Where" }, spec.PipelineSteps.Select(step => step.Operation));
        Assert.Throws<InvalidOperationException>(() =>
            spec.EnsureCompatibleWith(Specification.Create<Student>()));
        Assert.Throws<InvalidOperationException>(() =>
            spec.And(Specification.Create<Student>()));
    }

    [Fact]
    public void IndependentBranchesArePersistentAndEvaluationIsDeferred()
    {
        var enumerations = 0;
        IEnumerable<Student> Enumerate()
        {
            enumerations++;
            foreach (var student in StudentsData()) yield return student;
        }

        var root = Specification.Create<Student>().SelectMany(s => s.Books);
        var first = root.Where(b => b.Name.StartsWith("A"));
        var second = root.Where(b => b.Name.StartsWith("B"));
        var query = Enumerate().BySpecification(first);

        Assert.Equal(0, enumerations);
        Assert.Equal(3, query.Count());
        Assert.Single(Enumerate().BySpecification(second));
        Assert.Equal(2, enumerations);
        Assert.Equal(1, root.PipelineSteps.Count);
    }
}
