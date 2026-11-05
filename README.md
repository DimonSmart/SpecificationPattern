# Specification Pattern for .NET 10

An immutable Specification Pattern implementation for LINQ and Entity Framework Core.

Packages:
- **DimonSmart.Specification** — filtering, sorting, paging and composition for `IQueryable<T>`, without Entity Framework dependencies.
- **DimonSmart.Specification.EntityFrameworkCore** — adds `Include`, `ThenInclude` and EF Core query options.

## Immutable specifications and runtime validation

The public API has a single immutable type, `Specification<T>` (or
`EFCoreSpecification<T>`). Configuration operations return a new specification,
so branches can be reused independently. A disabled conditional operation returns
the original instance.

`And` and `Or` merge compatible filters and configuration, rejecting conflicts
in ordering, paging, keyset boundaries, Include plans, and EF Core options
immediately with `InvalidOperationException`. Call
`left.EnsureCompatibleWith(right)` to check without composing.

### LINQ

```csharp
var filter = Specification.Create<Student>()
    .Where(s => s.Age >= 18);
var sorting = Specification.Create<Student>()
    .OrderBy(s => s.Name)
    .OrderByDesc(s => s.Age);

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

See [Migration to v2](docs/Migration-to-v2.md) for the breaking API changes.

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

For backwards compatibility, repeated `OrderBy` / `OrderByDesc` calls
still **append** keys, rather than replacing previous ordering. The old
and new forms can also be mixed, always preserving call order. Once
`SeekAfter` has been configured, adding any sort key (including `ThenBy`
or `ThenByDesc`) throws. With keyset pagination, all key values must
be supplied in order, including keys added through `ThenBy`.

The `SpecificationData.OrderExpressions` diagnostic property exposes a
**read-only, independent snapshot** of
`IReadOnlyList<(OrderDirectionEnum direction, LambdaExpression expr)>`.
Items retain their insertion order, direction and original selector;
`expr.ReturnType` reports the runtime key type (for example `typeof(int)`).
The diagnostic snapshot cannot alter query execution. The same information
is available through `EFCoreSpecification.SpecificationData`.

This changes the old non-generic ordering method signatures and the former
mutable `OrderExpressions` list API. Normal lambda expressions and explicitly
typed `Expression<Func<Student, int>>` selectors continue to work. A
user-supplied `Expression<Func<Student, object>>`, or an explicit cast
`s => (object)s.Age`, still has `TKey = object`; the library does not remove
that cast or guarantee database translation of user-authored conversions.
No library-generated boxing conversion is introduced for naturally typed keys.

Immutable branches, ordering conflicts in `And` / `Or`, and the application
order `Where` → sorting → `Skip` → `Take` → terminal `Select` are unchanged.

### Conditional ordering and pagination

Both packages support a `bool condition` overload for `OrderBy`, `OrderByDesc`,
`ThenBy`, `ThenByDesc`, `Skip`, and `Take`, matching the existing optional
`Where(predicate, condition)` pattern. For example:

```csharp
var spec = EFCoreSpecification.Create<Student>()
    .Where(s => s.Age >= 20)
    .OrderBy(s => s.Name, sortByName)
    .OrderByDesc(s => s.Age, sortByAge)
    .Skip(skip, skip > 0)
    .Take(take, take > 0);
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
the query. Terminal projections are supported by both forms.

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

`Select()` is terminal: configure query filters and compose specifications **before** projection. Explicit `Include` / `ThenInclude` and `Select` remain mutually incompatible in the current API.

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

## Projection / Select

Call `Select<TResult>(Expression<Func<TEntity, TResult>>)` after configuring a base LINQ or EF Core specification to construct a typed, terminal projection. `Where`, ordering, `Skip`, and `Take` operate on the original entity before `Queryable.Select`; enumeration and SQL execution remain deferred. There is no restriction on `TResult` (DTOs, anonymous types, scalars, entities and nullable types are supported).

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

Navigation properties such as `s.School.Name` can be projected directly without `Include`. Explicit `Include`, `ThenInclude`, and filtered Include plans **cannot** be combined with `Select` in this version: calling `Select` throws `InvalidOperationException` immediately. `IgnoreAutoIncludes` without an explicit Include is allowed.

A projected specification is **terminal** at the type level: there are no specification builder operations, composition or replacement `Select` after it. Compose with `And` / `Or` and configure all query options *before* calling `Select`. Standard `IQueryable<TResult>` operators remain available on the returned query.

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
                .OrderBy(s => s.Id)
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
the entire resulting filter (including `Or`), before ordering, `Take`, and `Select`.
There is no backward pagination or token serialization in v1.

## Supported operations

| Query operation | LINQ | EF Core |
|---|---|---|
| `Where`, `And`, `Or` | Yes | Yes |
| `Select` (terminal projection) | Yes | Yes |
| `OrderBy`, `OrderByDesc` | Yes | Yes |
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
