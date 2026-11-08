# Specification Pattern for .NET 10

An immutable Specification Pattern implementation for LINQ and Entity Framework Core.

Packages:
- **DimonSmart.Specification** — an ordered, immutable LINQ pipeline over `IQueryable<T>` and `IEnumerable<T>`, with typed `Select` / `SelectMany` and canonical composition.
- **DimonSmart.Specification.EntityFrameworkCore** — adds `Include`, `ThenInclude` and EF Core query options.


## String navigation paths in EF Core

`Include("Books.Author")` accepts a CLR navigation path from a string,
including paths through collection navigations. It also supports the conditional
form `Include(path, condition)`.

```csharp
var spec = EFCoreSpecification.Create<Student>()
    .Include("Books.Author")
    .Include(s => s.School)
    .AsNoTracking();
```

String paths share the same immutable IncludePlan merging, prefix deduplication,
diagnostics, and conflict detection as typed `Include` / `ThenInclude`.
A string Include returns `EFCoreSpecification<T>`; use typed Include if
you want to continue with a typed `ThenInclude` chain.

Paths are resolved against **public CLR properties and fields** (including
navigation properties behind `IEnumerable<T>`) when building the specification.
Invalid or empty paths fail early; EF Core validates that these CLR members
are mapped navigations when executing the query. Shadow-only navigations
without CLR members are not supported, since they cannot be merged safely
with the typed Include paths.

## EF Core SQL LIKE search

`Search(selector, pattern)` is available on root and projected EF Core
specifications. It adds a normal `Where` step using `EF.Functions.Like`
and composes with other filters and `And`/`Or` (where their ordinary
composition restrictions allow). `Search(selector, pattern, condition)`
skips the operation when `condition` is false.

```csharp
var matches = EFCoreSpecification.Create<Student>()
    .Search(s => s.Name, "%Alex%")
    .OrderBy(s => s.Name);
var query = dbContext.BySpecification(matches);
```

The pattern uses **SQL LIKE syntax**, not a plain substring:
`%` matches any sequence, and `_` matches one character. Comparison,
case sensitivity and escaping depend on the database provider. No implicit
wildcards or client-side `Contains` operations are added. `Search` is for
relational EF Core query providers; it should not be evaluated through
`IsSatisfiedBy` / LINQ-to-Objects because `EF.Functions.Like` requires
provider translation.

## Checking an entity with IsSatisfiedBy

`IsSatisfiedBy(candidate)` is available on `Specification<T>`,
`EFCoreSpecification<T>` and their `ISpecification<T>` contract.
It evaluates only the specification's filtering criteria against one entity,
using the same ordered criteria-only pipeline as `BySpecificationCriteria`.
Sorting, `Skip`, `Take` and `SeekAfter` do not affect the answer.
An unrestricted specification matches every non-null entity.

```csharp
var adults = Specification.Create<Student>().Where(s => s.Age >= 18);
bool isAdult = adults.IsSatisfiedBy(student);
```

The evaluation uses LINQ-to-Objects, compiling the combined `Where`
predicate lazily **once per immutable specification** and reusing the delegate
across calls. It retains short-circuit semantics and supports noncanonical
pipeline order (such as `Take(0).Where(...)`). Derived immutable
specifications sharing the exact same core pipeline can reuse the cache.

For EF Core specifications, EF-only
settings (including database global query filters) are not applied, and
provider-specific expressions such as `EF.Functions.Like` may fail in memory.
Use a database query to check database-specific predicates.

## Reusable interfaces and projections

`ISpecification<T>` and `ISpecification<TSource, TResult>` provide immutable,
read-only execution contracts for generic consumers. EF Core equivalents
`IEFCoreSpecification<T>` and `IEFCoreSpecification<TSource, TResult>`
also expose EF option diagnostics. Their `ApplyTo` and `ApplyCriteriaTo`
methods accept an existing `IQueryable` without materializing results.
`BySpecification` and `BySpecificationCriteria` accept the interfaces
on `IQueryable`, and EF-specific interfaces on `DbContext`.

```csharp
ISpecification<Student> adults = Specification.Create<Student>()
    .Where(s => s.Age >= 18);
var matching = students.BySpecification(adults);
```

`WithProjectionOf` on a normal specification accepts a previously
constructed `ProjectedSpecification<TSource, TResult>`, and the EF Core
variant accepts an `EFCoreProjectedSpecification<TSource, TResult>`.
It creates a new specification by **appending the entire supplied projected
pipeline** after the source pipeline, without materialization. This includes
any filters and pagination within the supplied projected specification;
it is intentionally more than just copying a final selector. All operations
retain their LINQ v3 order. For reuse of only a mapping, define the
projection with `Select` or `SelectMany` but no additional query operations.

```csharp
var names = EFCoreSpecification.Create<Student>()
    .Select(s => s.Name);

var adults = EFCoreSpecification.Create<Student>()
    .Where(s => s.Age >= 18);

var adultNames = adults.WithProjectionOf(names);
var result = dbContext.BySpecification(adultNames);
```

The original filter and projection remain unchanged. `PipelineSteps`
shows the combined operations. The EF Core overload merges root options
and rejects conflicting modes and explicit `Include` plans. It does not
silently ignore explicit loading while projecting.

## Conditional EF Core query options and Includes

`Include(expression, condition)` and `ThenInclude(expression, condition)`
conditionally add navigation paths. A disabled `Include` starts an inactive
typed chain, so following `ThenInclude` calls do not add navigation paths.
Disabling a `ThenInclude` preserves previously added paths but stops that
particular continuation. Independent Include paths are unaffected.

```csharp
var spec = EFCoreSpecification.Create<Student>()
    .Include(s => s.Books, includeBooks)
    .ThenInclude(b => b.Author)
    .AsNoTracking(noTracking)
    .AsSplitQuery(splitQuery)
    .TagWith("Student query", debugSql);
```

The options `AsTracking`, `AsNoTracking`,
`AsNoTrackingWithIdentityResolution`, `AsSingleQuery`, `AsSplitQuery`,
`IgnoreAutoIncludes`, `IgnoreQueryFilters` (all filters), and `TagWith`
now have conditional overloads. Disabled options return the original
instance; enabled options behave like their unconditional counterparts.
Even disabled operations validate non-null selectors and tags.

## Immutable specifications and runtime validation

The public API has a single immutable type, `Specification<T>` (or
`EFCoreSpecification<T>`). Configuration operations return a new specification,
so branches can be reused independently. A disabled conditional operation returns
the original instance.

`And` and `Or` merge compatible filters and configuration, rejecting conflicts
in ordering, paging, keyset boundaries, Include plans, and EF Core options
immediately with `InvalidOperationException`. Call
`left.EnsureCompatibleWith(right)` to check without composing.

A specification without a `Where` clause represents an unrestricted predicate
(`true`). It is neutral for `And`, but absorbing for `Or`: `filter.Or(unfiltered)`
and `unfiltered.Or(filter)` both match all entities. Other compatible query
settings (ordering, paging, and EF Core options) are still merged; incompatible
settings still throw. To build an `Or` expression incrementally, start from the
first actual predicate, or from an explicit `Where(x => false)` seed, rather
than from `Specification.Create<T>()`.

### LINQ

```csharp
var filter = Specification.Create<Student>()
    .Where(s => s.Age >= 18);
var sorting = Specification.Create<Student>()
    .OrderBy(s => s.Name)
    .ThenByDesc(s => s.Age);

Specification<Student> combined = filter.And(sorting);
var result = students.BySpecification(combined).ToList();
```


### Dynamic configuration with a single variable

You can add filters, sorting, pagination and EF Core settings without changing
the static type, using a mutable local variable but **immutable specifications**.

The following example is compiled as
[`DynamicSpecificationExample.cs`](DimonSmart.Specification.Tests/Documentation/DynamicSpecificationExample.cs).

<!-- compiled-example: DynamicSpecificationExample -->
```csharp
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
```

EF Core uses the same pattern for `Include`, tracking and other query options.
This example is compiled as
[`DynamicEFCoreSpecificationExample.cs`](DimonSmart.Specification.EntityFrameworkCore.Db.Tests/Documentation/DynamicEFCoreSpecificationExample.cs).

<!-- compiled-example: DynamicEFCoreSpecificationExample -->
```csharp
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
```

See [Migration to v3](docs/Migration-to-v3.md) for changes to LINQ operator ordering, composition, keyset pagination and diagnostics. The [v2 guide](docs/Migration-to-v2.md) remains available.

### IEnumerable overloads (no AsQueryable required)

Both the ordinary and projected `BySpecification` methods also accept `IEnumerable<T>`
directly, including `List<T>`, arrays and lazy sequences. The methods call
`AsQueryable()` internally and reuse the existing LINQ specification evaluator.
This is syntactic sugar: no new evaluator, eager materialization, or additional
dependency is introduced.

For `IQueryable<T>` sources (including `DbSet<T>`), the existing, more-specific
extension overload remains selected and returns `IQueryable<T>`. Sources
whose compile-time type is `IEnumerable<T>` return `IEnumerable<T>`.
Normal LINQ-to-Objects semantics apply when the underlying source is in memory;
database-specific expressions may not be executable in memory.

The following example is compiled as
[`EnumerableBySpecificationExample.cs`](DimonSmart.Specification.Tests/Documentation/EnumerableBySpecificationExample.cs)
and checked against README by CI.

<!-- compiled-example: EnumerableBySpecificationExample -->
```csharp
using DimonSmart.Specification;
using StudentEntity = DimonSmart.Specification.Tests.Common.Student;

namespace DocumentationExamples;

public sealed record StudentAgeView(int Id, int Age);

public static class EnumerableBySpecificationExample
{
    public static IEnumerable<StudentEntity> SelectAdults(IEnumerable<StudentEntity> students)
    {
        var specification = Specification.Create<StudentEntity>()
            .Where(s => s.Age >= 18)
            .OrderBy(s => s.Age)
            .Take(10);

        return students.BySpecification(specification);
    }

    public static IEnumerable<StudentAgeView> SelectAdultAges(IEnumerable<StudentEntity> students)
    {
        var specification = Specification.Create<StudentEntity>()
            .Where(s => s.Age >= 18)
            .OrderBy(s => s.Id)
            .Select(s => new StudentAgeView(s.Id, s.Age));

        return students.BySpecification(specification);
    }
}
```

### Typed ordering

Both packages support typed `OrderBy<TKey>` / `OrderByDesc<TKey>` for the
primary key and explicit `ThenBy<TKey>` / `ThenByDesc<TKey>` for subsequent
keys. All selectors retain their original expression trees and inferred key types
without library-generated conversions to `object`.

This example is compiled as
[`TypedOrderingExample.cs`](DimonSmart.Specification.Tests/Documentation/TypedOrderingExample.cs)
and checked against the README by CI.

<!-- compiled-example: TypedOrderingExample -->
```csharp
using DimonSmart.Specification;
using StudentEntity = DimonSmart.Specification.Tests.Common.Student;

namespace DocumentationExamples;

public static class TypedOrderingExample
{
    public static IQueryable<StudentEntity> Query(IQueryable<StudentEntity> students)
    {
        var spec = Specification.Create<StudentEntity>()
            .OrderBy(s => s.Name)
            .ThenByDesc(s => s.Age)
            .ThenBy(s => s.Id);

        return students.BySpecification(spec);
    }
}
```

The first key uses LINQ `Queryable.OrderBy` or `OrderByDescending`;
subsequent keys use `Queryable.ThenBy` or `ThenByDescending`.
`ThenBy` adds an ascending key and `ThenByDesc` adds a descending key.
Both require an existing `OrderBy` or `OrderByDesc`: calling either without
an ordering throws `InvalidOperationException`, even on a specification
configured by `Take`, `Skip`, or EF Core query options.

Each `OrderBy` / `OrderByDesc` starts a new sorting block, just like standard
LINQ. Add secondary keys with `ThenBy` / `ThenByDesc`. These methods require
a **directly preceding active ordering block**: `OrderBy(...).Where(...).ThenBy(...)`
and `OrderBy(...).Take(...).ThenBy(...)` fail immediately. A disabled
conditional operation does not interrupt a block. After `SeekAfter`, adding
any sorting key in the same stage is forbidden. Keyset cursors must provide
values for all keys in the current block.

The `PipelineSteps` property is the single read-only diagnostic view for
both core and EF Core specifications. Ordering steps have `IsOrdering = true`,
expose their `Direction` and retain the original typed selector in
`Expressions[0]` (`ReturnType` gives the runtime key type).
`Skip` and `Take` steps expose their configured `Count`. The snapshot
cannot alter query execution.

The v4 diagnostic API removes the old `SpecificationData`,
`ISpecificationData<T>` and `GetWhereExpression()` APIs. Use
`PipelineSteps` to inspect operations and expressions in execution order.
Normal lambda expressions and explicitly
typed `Expression<Func<Student, int>>` selectors continue to work. A
user-supplied `Expression<Func<Student, object>>`, or an explicit cast
`s => (object)s.Age`, still has `TKey = object`; the library does not remove
that cast or guarantee database translation of user-authored conversions.
No library-generated boxing conversion is introduced for naturally typed keys.

Immutable branches and runtime configuration conflict checks remain. Unlike v2,
the evaluation order of `Where`, sorting, `Skip`, `Take`, `Select` and
`SelectMany` is exactly the order in which they are configured. For example,
`Take(10).Where(predicate)` filters only those ten input elements.

### Conditional ordering and pagination

Both packages support a `bool condition` overload for `OrderBy`, `OrderByDesc`,
`ThenBy`, `ThenByDesc`, `Skip`, and `Take`, matching the existing optional
`Where(predicate, condition)` pattern. For example:

```csharp
var spec = EFCoreSpecification.Create<Student>()
    .Where(s => s.Age >= 20);
if (sortByName)
    spec = spec.OrderBy(s => s.Name);
if (sortByAge)
    spec = sortByName ? spec.ThenByDesc(s => s.Age) : spec.OrderByDesc(s => s.Age);
spec = spec.Skip(skip, skip > 0).Take(take, take > 0);
```

A disabled operation adds no sorting or pagination, and never creates a
composition conflict. Enabled operations behave exactly like the corresponding
single-argument overloads. `ThenBy` and `ThenByDesc` require an **active**
primary ordering only when enabled. A disabled ordering or `Skip` can safely
follow `SeekAfter`; enabling such an operation still throws its usual error.
Null ordering selectors and negative pagination counts are rejected even when
their conditions are false.

A disabled operation returns the original instance without changing the static type.

### Custom specifications by inheritance

To define a reusable custom specification, derive from `Specification<T>`
and pass a configured specification to the protected constructor. The API is immutable:
calling `Where()` without using the returned value does not change the instance.

The following example is compiled as
[`CustomStudentSpecification.cs`](DimonSmart.Specification.Tests/Documentation/CustomStudentSpecification.cs).
CI checks that the code block and the compiled C# file match exactly.

<!-- compiled-example: CustomStudentSpecification -->
```csharp
using DimonSmart.Specification;

namespace DocumentationExamples;

public sealed record School(string Name);
public sealed record Student(int Age, School School);

public sealed class CustomStudentSpecification : Specification<Student>
{
    public CustomStudentSpecification(int minAge, string schoolName)
        : base(Specification.Create<Student>()
            .Where(s => s.Age >= minAge && s.School.Name == schoolName))
    {
    }
}
```

A `CustomStudentSpecification` can be used directly with `BySpecification()`
or combined with another specification using `And()` or `Or()`.

### Entity Framework Core

Named EF Core specifications can derive from `EFCoreSpecification<T>` by passing
an already configured immutable specification to the protected constructor.
The constructor preserves both LINQ operations and EF Core query options.

The following example is compiled as
[`CustomEFCoreStudentSpecification.cs`](DimonSmart.Specification.EntityFrameworkCore.Db.Tests/Documentation/CustomEFCoreStudentSpecification.cs)
and checked against README by CI.

<!-- compiled-example: CustomEFCoreStudentSpecification -->
```csharp
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
```

Fluent operations on a derived specification return `EFCoreSpecification<T>`
and never mutate the original named instance. Composition and conflict validation
work the same way as for factory-created specifications.


```csharp
var filter = EFCoreSpecification.Create<Student>()
    .Where(s => s.Age >= 18)
    .Include(s => s.Books)
    .ThenInclude(b => b.Author);

var configured = EFCoreSpecification.Create<Student>()
    .OrderBy(s => s.Name)
    .Take(10)
    .AsNoTracking();

var combined = filter.And(configured);
var result = dbContext.BySpecification(combined).ToList();
```

An `Include` result supports typed `ThenInclude` and remains usable as an ordinary `EFCoreSpecification<T>`.

`And` / `Or` combine `Where` expressions and compatible typed `Include` plans from **both** specifications (with duplicates and covered prefixes removed). Compatible query configurations are merged — for example, sorting from one operand and `Take` from the other.

Conflicts are rejected with `InvalidOperationException`: both operands specifying ordering, both specifying `Skip`, both specifying `Take`, incompatible tracking/split-query modes, or conflicting filtered Includes. Tags are appended as separate SQL comments in left-to-right order (including duplicates); ignore-filter options are combined.

The merged configuration applies to the **final combined query**, not separately to each branch of `Or`.

### Apply EF Core specifications to an existing IQueryable

EF Core specifications now work both with `DbContext.BySpecification(spec)` and
with `IQueryable<TEntity>.BySpecification(spec)`, including `DbSet<TEntity>`.
Use the queryable form to preserve filters or other operators already applied
to the source query. The existing DbContext overload delegates to the same
implementation using `context.Set<TEntity>()`; neither overload materializes
the query. Continuable projections and subsequent LINQ operators are supported by both forms.

The IQueryable overload requires an **EF Core query provider**. Ordinary
LINQ-to-Objects sources should instead use `Specification.Create<TEntity>()`
with the base LINQ / IEnumerable `BySpecification` overloads. EF Core-only
options such as `Include`, tracking, query filters and SQL tags must not
silently turn into no-ops on in-memory sources. A provider without
`IAsyncQueryProvider` is rejected immediately.

The following example is compiled as
[`QueryableEFCoreExample.cs`](DimonSmart.Specification.EntityFrameworkCore.Db.Tests/Documentation/QueryableEFCoreExample.cs)
and checked against README by CI.

<!-- compiled-example: QueryableEFCoreExample -->
```csharp
using DimonSmart.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudentEntity = DimonSmart.Specification.Tests.Common.Student;

namespace DocumentationExamples;

public static class QueryableEFCoreExample
{
    public static IQueryable<StudentEntity> Query(DbContext context, int minimumAge)
    {
        var specification = EFCoreSpecification.Create<StudentEntity>()
            .Where(s => s.Name.StartsWith("A"))
            .Include(s => s.School)
            .AsNoTracking()
            .OrderBy(s => s.Id)
            .Take(20);

        return context.Set<StudentEntity>()
            .Where(s => s.Age >= minimumAge)
            .BySpecification(specification);
    }

    public static IQueryable<string> Names(DbContext context, int minimumAge)
    {
        var specification = EFCoreSpecification.Create<StudentEntity>()
            .OrderBy(s => s.Id)
            .AsNoTracking()
            .Select(s => s.Name);

        return context.Set<StudentEntity>()
            .Where(s => s.Age >= minimumAge)
            .BySpecification(specification);
    }
}
```

Existing `Where` filters remain in the query and combine with the specification's
filters. Ordering, pagination, `SeekAfter`, EF Core Includes, tracking, query
filter options, split/single-query and tags still operate on the resulting
`IQueryable`. Query operators **already present** on the input are not
rewritten or removed: applying an additional `Where` after a prior `Take`
retains the prior `Take`. Apply specifications before paging on the source
when you want to page the combined results.

### Conditional Where

Both packages support `Where(predicate, condition)` for optional filters. A true
condition behaves exactly like `Where(predicate)` and combines consecutive filters
with AND. A false condition skips the predicate entirely and returns the same
immutable specification instance. The predicate is checked for null even when
the condition is false. The condition is evaluated in C# during specification
construction; it is not sent to SQL.

The following example is compiled as
[`ConditionalWhereExample.cs`](DimonSmart.Specification.EntityFrameworkCore.Db.Tests/Documentation/ConditionalWhereExample.cs)
and verified against README by CI.

<!-- compiled-example: ConditionalWhereExample -->
```csharp
using DimonSmart.Specification.EntityFrameworkCore;
using DimonSmart.Specification.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace DocumentationExamples;

public static class ConditionalWhereExample
{
    public static IQueryable<Student> Query(DbContext dbContext, string? name, int? maxAge)
    {
        var specification = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age >= 18)
            .Where(s => s.Name == name, !string.IsNullOrWhiteSpace(name))
            .Where(s => s.Age <= maxAge, maxAge.HasValue)
            .OrderBy(s => s.Id)
            .Take(20);

        return dbContext.BySpecification(specification);
    }
}
```

With missing `name` and `maxAge`, only the required age predicate is applied.
With values present, both optional predicates are added with AND.
### Filtered Include and composition

Matching **independently constructed** filtered Include operations are deduplicated,
including compatible `Include`/`ThenInclude` chains and `And`/`Or` composition.
Comparison is based on normalized expression trees, not on a random identifier
or `Expression.ToString()`. Different operators, constants or captured inputs
remain conflicting. Captured reference values are compared conservatively by
identity; unsupported expression node kinds are not merged without proof.
Filtered and unfiltered Include paths still conflict. EF Core itself executes
the original Include expressions; normalization is used only for conflict detection.


EF Core `Include` and `ThenInclude` are executed with **original typed expressions**, including filtered collections. Paths in `EFCoreSpecificationData.Includes` are read-only diagnostics, not executable inputs.

This example is compiled as [`FilteredIncludeExample.cs`](DimonSmart.Specification.EntityFrameworkCore.Db.Tests/Documentation/FilteredIncludeExample.cs). CI checks that this README block matches its compiled source.

<!-- compiled-example: FilteredIncludeExample -->
```csharp
using DimonSmart.Specification.EntityFrameworkCore;
using DimonSmart.Specification.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace DocumentationExamples;

public static class FilteredIncludeExample
{
    public static IQueryable<Student> Query(DbContext dbContext)
    {
        var books = EFCoreSpecification.Create<Student>()
            .Include(s => s.Books
                .Where(b => b.Name.StartsWith("A"))
                .OrderBy(b => b.Name)
                .Take(5))
            .ThenInclude(b => b.Author);

        var school = EFCoreSpecification.Create<Student>()
            .Include(s => s.School);

        return dbContext.BySpecification(books.And(school).AsNoTracking());
    }
}
```

Allowed collection filters: `Where`, `OrderBy`, `OrderByDescending`, `ThenBy`, `ThenByDescending`, `Skip`, `Take`. A filtered collection may be followed by `ThenInclude`; the collection element type is preserved. A collection reached via `ThenInclude` may itself be filtered.

Compatible paths are normalized: duplicate Includes are removed, longer chains cover their prefixes, and distinct child branches remain. **Strict conflict policy:** independently created filters on the same collection conflict, even when their expression text is identical. Filtered and unfiltered Includes for the same collection also conflict. Checks run at `Include`, `ThenInclude`, `And`, and `Or`, including shared intermediate collection segments. Branches derived from the **same** filtered Include share a descriptor and are compatible.

Only member-access paths and the seven collection operators listed above are recognized. `EF.Property`, shadow navigations, derived-type casts, `Select`, `Distinct` and arbitrary extension methods are unsupported in v1. EF Core validates whether members are mapped navigations and whether filter predicates translate to SQL. Provider restrictions still apply: for example, EF Core 10 with SQLite can reject filtered pagination followed by a child `ThenInclude` if its translation requires SQL `APPLY`. Successful compilation of the example checks the public API, not SQL translation on every provider.

### Named Query Filters (EF Core 10)

EF Core 10 supports multiple global query filters registered with `HasQueryFilter(name, predicate)`. The no-argument `.IgnoreQueryFilters()` disables **all** filters, including tenant isolation. The new `.IgnoreQueryFilters("SoftDeletionFilter")` disables **only** the named filter; e.g. `TenantFilter` remains active. Multiple names can be supplied and are accumulated by repeated calls and `And` / `Or` (left-to-right deduplicated); if either side disables all filters, the combined query disables all. Options apply once to the **entire resulting query**, not individual branches or entity types. Filter names are exact and case-sensitive in specification state; EF Core determines how they are resolved.

The example registers both filters and demonstrates selective and complete disabling, `And`, `Or`, and a projection. It is compiled as [`NamedQueryFiltersExample.cs`](DimonSmart.Specification.EntityFrameworkCore.Db.Tests/Documentation/NamedQueryFiltersExample.cs) and verified against this README in CI.

<!-- compiled-example: NamedQueryFiltersExample -->
```csharp
using DimonSmart.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DocumentationExamples;

public sealed class NamedFilterOrder
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public bool IsDeleted { get; set; }
    public int TotalInCents { get; set; }
}

public sealed class NamedFilterOrdersContext(
    DbContextOptions<NamedFilterOrdersContext> options, int tenantId) : DbContext(options)
{
    public int TenantId { get; } = tenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NamedFilterOrder>()
            .HasQueryFilter("SoftDeletionFilter", o => !o.IsDeleted)
            .HasQueryFilter("TenantFilter", o => o.TenantId == TenantId);
    }
}

public static class NamedQueryFiltersExample
{
    public static IQueryable<NamedFilterOrder> Query(NamedFilterOrdersContext dbContext)
    {
        var expensive = EFCoreSpecification.Create<NamedFilterOrder>()
            .Where(o => o.TotalInCents > 10000);
        var includeDeleted = EFCoreSpecification.Create<NamedFilterOrder>()
            .IgnoreQueryFilters("SoftDeletionFilter");

        return dbContext.BySpecification(expensive.And(includeDeleted)
            .OrderByDesc(o => o.Id).Take(20));
    }

    public static IQueryable<NamedFilterOrder> QueryEither(NamedFilterOrdersContext dbContext)
    {
        var deleted = EFCoreSpecification.Create<NamedFilterOrder>()
            .Where(o => o.IsDeleted).IgnoreQueryFilters("SoftDeletionFilter");
        var expensive = EFCoreSpecification.Create<NamedFilterOrder>()
            .Where(o => o.TotalInCents > 50000).IgnoreQueryFilters("SoftDeletionFilter");

        return dbContext.BySpecification(deleted.Or(expensive));
    }

    public static IQueryable<int> ProjectIds(NamedFilterOrdersContext dbContext)
    {
        var projection = EFCoreSpecification.Create<NamedFilterOrder>()
            .IgnoreQueryFilters("SoftDeletionFilter")
            .Select(o => o.Id);

        return dbContext.BySpecification(projection);
    }

    public static IQueryable<NamedFilterOrder> QueryWithoutAnyFilters(
        NamedFilterOrdersContext dbContext) =>
        dbContext.BySpecification(EFCoreSpecification.Create<NamedFilterOrder>()
            .IgnoreQueryFilters());
}
```

Configure EF Core query options and compose source specifications **before** projection. Later LINQ operations remain available on projected stages. Explicit `Include` / `ThenInclude` and `Select` remain mutually incompatible in the current API.

**Security:** Disabling `TenantFilter` can expose data from other tenants. These methods are query configuration, not authorization; only trusted application code should decide which filters may be bypassed. The library does not inspect or validate names against the EF model.

### Query options and tags

Tracking supports four states: default (use the `DbContext` setting), explicit `AsTracking()`, `AsNoTracking()`, and `AsNoTrackingWithIdentityResolution()`. `AsTracking()` enables change tracking even when `DbContext` defaults to `QueryTrackingBehavior.NoTracking`. Tracking and splitting (default/single/split) each have one internal state. Within one specification, the last setter wins. Across `And`/`Or`, identical explicit modes are compatible, different explicit modes conflict, and default is neutral. The diagnostic snapshot exposes `IsAsTracking`, `IsAsNoTracking`, and `IsAsNoTrackingWithIdentityResolution`.

This example is compiled as [`TrackingExample.cs`](DimonSmart.Specification.EntityFrameworkCore.Db.Tests/Documentation/TrackingExample.cs) and checked by CI.

<!-- compiled-example: TrackingExample -->
```csharp
using DimonSmart.Specification.EntityFrameworkCore;
using DimonSmart.Specification.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace DocumentationExamples;

public static class TrackingExample
{
    public static IQueryable<Student> Query(DbContext dbContext)
    {
        var specification = EFCoreSpecification.Create<Student>()
            .Where(s => s.Age >= 18)
            .AsTracking();

        return dbContext.BySpecification(specification);
    }
}
```

`TagWith("A").TagWith("B")` generates separate ordered SQL comments. Duplicate tags remain in immutable specification state (EF Core 10 may deduplicate identical SQL comments), but empty/whitespace-only strings are ignored (the specification remains unchanged if the tag is blank). `TagWith(null)` throws. `TagWithCallSite("A")` adds one user tag plus one call-site tag with caller method, file and line; the file path may be absolute and is visible in SQL.

## Criteria-only evaluation

Use `BySpecificationCriteria(specification)` to reuse an immutable specification
when counting or checking all matching entities rather than a single page.
It is available for LINQ `IQueryable<T>` and `IEnumerable<T>`, as well as EF Core
`IQueryable<T>` and `DbContext`. Execution remains deferred.

The following example is compiled as
[`CriteriaOnlyEvaluationExample.cs`](DimonSmart.Specification.EntityFrameworkCore.Db.Tests/Documentation/CriteriaOnlyEvaluationExample.cs)
and checked against README by CI.

<!-- compiled-example: CriteriaOnlyEvaluationExample -->
```csharp
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
```

`total` counts all students matching the `Where` criteria, while `students`
contains the requested page. Both calls reuse the same immutable specification.

Criteria-only removes **only the specification's** sorting (`OrderBy`/`ThenBy`),
paging (`Skip`/`Take`), cursor boundary (`SeekAfter`), and EF Core
`Include`/`ThenInclude`, tracking, splitting, automatic-include override and
query tags. The cursor's internally generated `Where` is not a criterion.
EF Core global query filters still apply unless disabled with
`IgnoreQueryFilters()` or the named-filter overload; only the named filters
specified are disabled. This follows the membership semantics of EF Core.

**Existing operations on the incoming source are preserved**, including
pre-existing `Where`, `OrderBy`, `Take`, or `Include` calls. Criteria-only
does not strip or rewrite the original `IQueryable` expression. It does not
bypass compatibility validation when specifications are composed with `And`
or `Or`. For a projected pipeline, `Select`, both forms of `SelectMany`,
and every `Where` are preserved, even if they originally followed pagination.
Counting therefore counts elements of the final projected type, not necessarily
the original entities.

## Projection / SelectMany / Select

Both `Select` and `SelectMany` start new typed stages; additional `Where`,
`OrderBy`, `ThenBy`, `Skip`, `Take`, `SeekAfter`, `Select`, and `SelectMany`
operations can follow on the current element type. There is no restriction on
the projected type (DTOs, anonymous types, scalars, entities, and nullable types
are supported). The pipeline remains deferred, using ordinary `Queryable`
expression trees without materialization.

### LINQ projection

This example is compiled as [`ProjectionExample.cs`](DimonSmart.Specification.Tests/Documentation/ProjectionExample.cs) and checked by CI.

<!-- compiled-example: ProjectionExample -->
```csharp
using DimonSmart.Specification;
using StudentEntity = DimonSmart.Specification.Tests.Common.Student;

namespace DocumentationExamples;

public sealed record StudentSummary(int Id, string Name);

public static class ProjectionExample
{
    public static IQueryable<StudentSummary> Query(IQueryable<StudentEntity> students)
    {
        var adults = Specification.Create<StudentEntity>().Where(s => s.Age >= 18);
        var page = Specification.Create<StudentEntity>()
            .OrderBy(s => s.Name).Skip(10).Take(20);

        var projection = adults.And(page)
            .Select(s => new StudentSummary(s.Id, s.Name));

        return students.BySpecification(projection);
    }
}
```

### EF Core DTO projection

This example is compiled as [`EFCoreProjectionExample.cs`](DimonSmart.Specification.EntityFrameworkCore.Db.Tests/Documentation/EFCoreProjectionExample.cs) and checked by CI.

<!-- compiled-example: EFCoreProjectionExample -->
```csharp
using DimonSmart.Specification.EntityFrameworkCore;
using DimonSmart.Specification.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace DocumentationExamples;

public sealed record StudentDto(int Id, string Name, string SchoolName);

public static class EFCoreProjectionExample
{
    public static async Task<List<StudentDto>> QueryAsync(DbContext dbContext)
    {
        var adults = EFCoreSpecification.Create<Student>().Where(s => s.Age >= 18);
        var page = EFCoreSpecification.Create<Student>()
            .OrderBy(s => s.Name).Skip(10).Take(20).AsNoTracking();

        var projection = adults.And(page)
            .Select(s => new StudentDto(s.Id, s.Name, s.School.Name));

        return await dbContext.BySpecification(projection).ToListAsync();
    }
}
```

Navigation properties such as `s.School.Name` can be projected directly without `Include`. Explicit `Include`, `ThenInclude`, and filtered Include plans **cannot** be combined with `Select` or `SelectMany`; both reject them immediately. `IgnoreAutoIncludes` without an explicit Include is allowed.

A projected specification is **continuable**. Compose with `And` / `Or` and
configure EF Core options before projection: arbitrary `And` / `Or` composition
between projected specifications is intentionally unsupported. A new `Select`
or `SelectMany` opens a fresh ordering/keyset stage, even when its CLR type
equals the previous stage.

The library passes the selector expression to the LINQ provider: it does not use `Func`, `Expression.Compile()` or intermediate entity materialization. SQL translation, client evaluation of top-level projections, and support for nested collections depend on EF Core and the database provider. Simple DTO projections can fetch only the columns needed by the selector. Tracking follows normal EF Core rules: scalar-only DTOs do not track entities, but DTOs containing entity instances can track those instances unless a no-tracking option was selected. `AsSplitQuery`, `AsSingleQuery`, query filters, auto-include options, and query tags are retained.

## Keyset pagination (forward only)

`SeekAfter` continues after the supplied values in the ordering defined by `OrderBy` /
`OrderByDesc`. Use a single value for one ordering key and a tuple for multiple keys.
Both LINQ and EF Core specifications are supported. The ordering must be **unique**
(e.g. include the entity Id as the last key), otherwise ties can be skipped.

This example is compiled as
[`KeysetPaginationExample.cs`](DimonSmart.Specification.EntityFrameworkCore.Db.Tests/Documentation/KeysetPaginationExample.cs)
and verified against README by CI.

<!-- compiled-example: KeysetPaginationExample -->
```csharp
using DimonSmart.Specification.EntityFrameworkCore;
using DimonSmart.Specification.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace DocumentationExamples;

public static class KeysetPaginationExample
{
    public static IQueryable<Student> NextPage(
        DbContext dbContext, int lastAge, int lastId) =>
        dbContext.BySpecification(
            EFCoreSpecification.Create<Student>()
                .Where(s => s.Age >= 18)
                .OrderByDesc(s => s.Age)
                .ThenBy(s => s.Id)
                .SeekAfter((lastAge, lastId))
                .Take(20));
}
```

The forward boundary is *exclusive*. Ascending and descending directions can be mixed.
Values must match the key types exactly. Version 1 supports `byte`, `short`,
`int`, `long`, `decimal`, `DateTime`, and `DateOnly`; string, nullable,
Guid and other key types are intentionally not supported yet. `SeekAfter`
cannot be combined with `Skip`, repeated, or followed by additional sorting.
A cursor participates in `And`/`Or` as a query-level option and applies to
the entire resulting filter (including `Or`) before the current ordering block.
It cannot be preceded by `Take` in the same stage, or mixed with `Skip`
in that stage. A new projected stage can have its own independent cursor.
There is no backward pagination or token serialization in v1.

### Typed SelectMany example

```csharp
var spec = EFCoreSpecification.Create<Student>()
    .Where(s => s.Age >= 18)
    .SelectMany(s => s.Books)
    .Where(book => book.Name.StartsWith("A"))
    .OrderBy(book => book.Name)
    .Take(10);

IQueryable<Book> books = dbContext.Set<Student>().BySpecification(spec);
int total = await dbContext.BySpecificationCriteria(spec).CountAsync();
```

`SelectMany(collectionSelector, resultSelector)` is also supported. The
`PipelineSteps` snapshot exposes ordered operations, stage IDs, element
types, original expression trees, ordering direction and paging counts.
The old source-stage `SpecificationData` diagnostic API has been removed
in v4; see [Migration to v4](docs/Migration-to-v4.md).

## Supported operations

| Query operation | LINQ | EF Core |
|---|---|---|
| `Where`, `And`, `Or` | Yes | Yes |
| `Select`, `SelectMany` (continuable stages) | Yes | Yes |
| `OrderBy`, `OrderByDesc`, `ThenBy`, `ThenByDesc` | Yes | Yes |
| `SeekAfter` (forward keyset pagination) | Yes | Yes |
| `Skip`, `Take` | Yes | Yes |
| `Include`, `ThenInclude` | — | Yes |
| `AsTracking`, `AsNoTracking`, `AsNoTrackingWithIdentityResolution` | — | Yes |
| `AsSplitQuery`, `AsSingleQuery` | — | Yes |
| `IgnoreAutoIncludes`, `IgnoreQueryFilters` | — | Yes |
| `TagWith`, `TagWithCallSite` | — | Yes |

Use `Specification.Create<T>()` for LINQ queries, or `EFCoreSpecification.Create<T>()` for EF Core. The former mutable one-parameter specification types have been removed.

## Build and test

Requires .NET 10 SDK.

```bash
dotnet restore DimonSmart.Specification.sln
dotnet test DimonSmart.Specification.sln --configuration Release
```
