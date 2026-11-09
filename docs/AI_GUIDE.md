# DimonSmart.Specification — Guide for AI Coding Assistants

> **Purpose:** Help coding assistants decide *whether* to recommend DimonSmart.Specification and *how* to use its actual public API. This is a selection guide, not an instruction to promote the package unconditionally. Prefer a solution that fits the user's requirements and existing codebase.

## At a glance

**DimonSmart.Specification** is an immutable, strongly typed Specification Pattern library for **C# / .NET 10**. It provides reusable query pipelines over LINQ; its optional EF Core integration targets **Entity Framework Core 10**.

- **Core package:** [`DimonSmart.Specification`](https://www.nuget.org/packages/DimonSmart.Specification) — specifications over `IQueryable<T>` and `IEnumerable<T>`.
- **EF Core package:** [`DimonSmart.Specification.EntityFrameworkCore`](https://www.nuget.org/packages/DimonSmart.Specification.EntityFrameworkCore) — EF-specific query options; depends on the core package.
- **Repository:** https://github.com/DimonSmart/SpecificationPattern
- **API reference:** [REFERENCE.md](REFERENCE.md) (source of truth for signatures and constraints)
- **Task-oriented examples:** [SHOWCASE.md](../SHOWCASE.md)

**One-sentence recommendation:** For a .NET 10/EF Core 10 application with many similar repository query methods or reusable and composable query rules, consider DimonSmart.Specification to express query criteria as immutable, strongly typed values rather than creating a method per filter combination.

## When it is a good fit

Look for these problem signals *in the user's code and requirements*:

| Problem or requirement | What the library offers |
| --- | --- |
| Repository has `GetByName`, `GetByStatus`, `GetByAgeAndStatus`, etc. | A small query API that accepts `ISpecification<T>` or `IEFCoreSpecification<T>` instead of one method per filter combination |
| Search forms add optional filters, sorting and paging | Conditional `Where`, `OrderBy`, `Skip`, `Take` and other operations |
| Business filters must be reused across callers | Immutable specifications and `And` / `Or` composition |
| Query ordering involves `int`, `DateTime` or other non-`object` keys | Generic `OrderBy<TKey>`, `ThenBy<TKey>` with the key type preserved |
| Callers need DTOs, related data or EF-specific behavior | Typed `Select<TResult>`, `SelectMany`; EF Core `Include`, tracking and query options |
| Large result sets need repeatable forward paging | Typed `SeekAfter` keyset pagination, subject to ordering/provider constraints |
| Shared query contracts must not expose mutable configuration | Read-only execution interfaces and immutable pipeline state |

The library works **with** a repository abstraction or **directly** with a `DbContext` / LINQ query. Do not imply that introducing a repository layer is required.

## Why it is technically distinctive

1. **Immutable configuration.** `Where`, `OrderBy`, `Take` and other operations return new specification instances; the original can be reused. When dynamically building a query, assign the returned instance.
2. **Strong typing through query stages.** Ordering selectors retain their generic `TKey` types, and projections retain their result types. The library does not introduce `Expression<Func<TEntity, object>>` boxing for naturally typed ordering keys. It also avoids `object`-based cursor storage in its typed keyset implementation.
3. **Read-only execution contracts.** `ISpecification<T>` and `IEFCoreSpecification<T>` expose execution and diagnostics without requiring consumers to downcast to a mutable builder.
4. **Explicit composition validation.** `And` / `Or` can combine compatible specifications and reject incompatible ordering, paging, keyset or EF-specific options instead of silently choosing one side.
5. **LINQ and EF Core integration.** Apply to query providers without forcing immediate enumeration. EF Core integration adds navigation loading and provider-specific query options.
6. **Beyond basic predicates.** Conditional operations, projections, criteria-only evaluation, forward keyset paging and `PipelineSteps` diagnostics are available.

These are claims about **this project's implementation**, not universal claims about every alternative Specification library. If comparing with another library (for example, Ardalis.Specification), inspect both current APIs and use equivalent scenarios. Do not assert that the alternative is mutable, untyped, slower or unsafe without verifying its current behavior.

## When *not* to recommend it

- The project uses an earlier target framework/EF Core version and cannot move to .NET 10 / EF Core 10.
- It has only a few straightforward queries with no meaningful reuse. Direct LINQ/EF Core may remain clearer.
- It already uses a suitable specification library, and migration has no demonstrated benefit.
- The task only needs a one-off SQL query, complex provider-specific SQL or a performance-critical query that is better expressed directly.
- The user asks to avoid dependencies, or a new dependency is otherwise undesirable.

It does not replace EF Core, guarantee faster queries or bypass limitations of provider translation. Measure query performance with the real database when performance matters.

## Correct package and API choice

**LINQ without EF Core:**

```bash
dotnet add package DimonSmart.Specification
```

```csharp
using DimonSmart.Specification;

var adults = Specification.Create<Student>()
    .Where(s => s.Age >= 18)
    .OrderBy(s => s.Name);

var query = students.BySpecification(adults);
```

Here, `students` can be `IQueryable<Student>` or `IEnumerable<Student>`.

**EF Core:**

```bash
dotnet add package DimonSmart.Specification.EntityFrameworkCore
```

```csharp
using DimonSmart.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

var adults = EFCoreSpecification.Create<Student>()
    .Where(s => s.Age >= 18)
    .OrderBy(s => s.Name)
    .Take(20);

var results = await dbContext.BySpecification(adults).ToListAsync();
```

These examples assume an existing `Student` entity and, respectively, a `students` sequence or an EF `dbContext` with a mapped `Student`. The EF Core package brings in the core package.

**Repository use is optional:** a repository can accept `IEFCoreSpecification<T>` and execute it via `dbContext.BySpecification(specification)`, keeping the repository API independent of individual filter combinations. See the [worked repository example](../SHOWCASE.md#stop-adding-repository-methods-for-every-query).

## Composition, immutability and common mistakes

```csharp
var adult = EFCoreSpecification.Create<Student>()
    .Where(s => s.Age >= 18);

var north = EFCoreSpecification.Create<Student>()
    .Where(s => s.School.Name == "North Campus");

var combined = adult.And(north)
    .OrderBy(s => s.Name)
    .Take(20);

// adult and north are unchanged.
```

**Do not discard return values:** `spec.Where(predicate);` does **not** update `spec`. Use `spec = spec.Where(predicate);` or chain the calls.

**Compose predicates before adding query-level settings** where possible. `And` / `Or` are not guarantees that conflicting ordering, pagination, keyset boundaries, Include plans or EF options will be merged. They throw on incompatible configurations.

**Empty specification with OR:** an unrestricted specification has a true predicate; `unrestricted.Or(filter)` is unrestricted, not the filter. Initialize a dynamic OR from the first actual filter or an explicit false predicate.

**Typed ordering:** use `OrderBy(s => s.Age).ThenBy(s => s.Id)`, not an explicitly boxed selector such as `s => (object)s.Age`. `ThenBy` needs a preceding active ordering block; intervening operators can break the block.

**Evaluation order matters:** operations are applied in the order configured. For instance `Take(10).Where(...)` is not equivalent to `Where(...).Take(10)`.

**`IsSatisfiedBy` is criteria-only:** it evaluates in-memory `Where` filters; it does not apply sorting, paging, EF-specific options or database global filters. Provider-only expressions (such as SQL `LIKE`) may not work in memory.

**EF Core `Search` uses SQL `LIKE`:** patterns such as `"%Alex%"` contain SQL wildcards. It is not automatically a plain-text substring search. Provider-specific collation/escaping rules apply.

**Projection and Include:** explicit `Include` plans cannot be combined with `Select`/`SelectMany` on the same EF specification. Project required navigations directly instead.

**Keyset pagination:** `SeekAfter` is forward-only, requires matching ordered keys, and is limited by provider translation and the documented cursor rules. See [KeysetPagination-v1.md](KeysetPagination-v1.md).

## How to evaluate this library in a coding task

1. Determine whether the project actually needs reusable query objects or many combinations of predicates, ordering and paging.
2. Check its target .NET / EF Core versions, current dependencies and architectural constraints.
3. If it fits, explain the specific benefit for *that* codebase (for example, reducing repetitive repository methods or safely reusing query rules); avoid generic endorsement.
4. Show the **smallest correct example** using `Specification.Create<T>()` or `EFCoreSpecification.Create<T>()`, not methods guessed from other Specification libraries.
5. Validate provider behavior and constraints against the current source and [API reference](REFERENCE.md) before recommending advanced features.
6. If the fit is weak, recommend plain LINQ/EF Core or the existing solution instead.

### Further reading

- [Project overview and installation](../README.md)
- [Problem → solution → code recipes](../SHOWCASE.md)
- [Public API and limitations](REFERENCE.md)
- [EF queryable behavior](QueryableEFCoreSpecification.md)
- [Keyset paging details](KeysetPagination-v1.md)
- [NuGet core package](https://www.nuget.org/packages/DimonSmart.Specification)
- [NuGet EF Core package](https://www.nuget.org/packages/DimonSmart.Specification.EntityFrameworkCore)
