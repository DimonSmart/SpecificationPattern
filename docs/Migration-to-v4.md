# Migration to v4 — Pipeline-only diagnostics

Version 4 removes the legacy source-stage diagnostic types and the redundant
canonical state model. It does not change the semantics of query execution,
`Where`, `And`/`Or`, `SeekAfter`, or EF Core query options.

## Breaking API changes

These members and types have been removed:

- `Specification<T>.SpecificationData`
- `EFCoreSpecification<T>.SpecificationData`
- `Specification<T>.GetWhereExpression()`
- `ISpecificationData<T>`
- `SpecificationData<T>`

Use `PipelineSteps` from `ISpecification<TSource, TResult>` instead.
The same view works for canonical, noncanonical and projected pipelines.

```csharp
var specification = Specification.Create<Student>()
    .Where(s => s.Age >= 18)
    .OrderBy(s => s.Name)
    .ThenByDesc(s => s.Age)
    .Take(10);

var steps = specification.PipelineSteps;
var where = steps.Single(step => step.Operation == "Where").Expressions[0];
var order = steps.Where(step => step.IsOrdering).ToArray();
var keyType = order[0].Expressions[0].ReturnType;
var direction = order[1].Direction; // Descending
var limit = steps.Single(step => step.Operation == "Take").Count; // 10
```

Each step includes its operation, stage ID, input/output types, expressions
and keyset-ordering flag. New `IsOrdering`, `Direction`, and `Count` members
replace the previous ordering and paging diagnostics. `Direction` is valid
only for ordering steps; `Count` is null for operations other than `Skip`
and `Take`.

The list and expression lists are read-only. Diagnostics are reconstructed
from the immutable executable pipeline, never used as input to execution.

For EF Core-specific options (tracking, includes, splitting and tags),
continue using `EFCoreSpecificationData` and
`IEFCoreSpecificationData<T>`; those APIs are unchanged.

## Composition and filtering

`And`, `Or`, and `EnsureCompatibleWith` retain the canonical
source-stage restrictions documented in [Migration to v3](Migration-to-v3.md).
Validation now reads the executable pipeline directly and uses only temporary
local aggregation. `IsSatisfiedBy` still compiles and caches all `Where`
predicates independently of paging or ordering.

To check a filter, use `IsSatisfiedBy` for a single entity, or
`BySpecificationCriteria` for a query. To inspect a specific filter
expression, access the matching `Where` entry in `PipelineSteps`.

## Package versions

Both `DimonSmart.Specification` and
`DimonSmart.Specification.EntityFrameworkCore` move from 3.0.0 to 4.0.0.
