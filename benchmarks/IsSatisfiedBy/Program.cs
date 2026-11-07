using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using DimonSmart.Specification;

BenchmarkRunner.Run<IsSatisfiedByBenchmarks>();

[MemoryDiagnoser]
public class IsSatisfiedByBenchmarks
{
    public sealed class Student
    {
        public int Age { get; init; }
        public string Name { get; init; } = "";
    }

    private readonly Student _matching = new() { Age = 25, Name = "Alex" };
    private readonly Specification<Student> _specification = Specification.Create<Student>()
        .Where(s => s.Age >= 18)
        .OrderBy(s => s.Name)
        .Take(0)
        .Where(s => s.Name.StartsWith("A"));

    // Previous IsSatisfiedBy used this singleton IQueryable criteria-only evaluation.
    [Benchmark(Baseline = true)]
    public bool OriginalQueryableEvaluation() =>
        new[] { _matching }.AsQueryable().BySpecificationCriteria(_specification).Any();

    [Benchmark]
    public bool CachedCompiledPredicate() => _specification.IsSatisfiedBy(_matching);
}
