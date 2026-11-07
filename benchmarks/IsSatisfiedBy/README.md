# IsSatisfiedBy microbenchmark

This optional BenchmarkDotNet project measures the previous implementation
(single-element AsQueryable + criteria-only pipeline + Any) against the
lazy-compiled predicate used by `Specification<T>.IsSatisfiedBy`.

Run:

```sh
dotnet run --project benchmarks/IsSatisfiedBy/IsSatisfiedBy.csproj -c Release
```

The benchmark is intentionally outside the main solution and CI because it
is a performance measurement rather than a correctness test. Results depend on
hardware and runtime; do not assume a fixed speedup without measurement.
