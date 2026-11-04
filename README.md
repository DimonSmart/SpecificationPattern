# Specification Pattern for .NET 10

An immutable Specification Pattern implementation for LINQ and Entity Framework Core.

Packages:
- **DimonSmart.Specification** — filtering, sorting, paging and composition for `IQueryable<T>`, without Entity Framework dependencies.
- **DimonSmart.Specification.EntityFrameworkCore** — adds `Include`, `ThenInclude` and EF Core query options.

## Composable and Configured

The specification's generic state tracks whether it contains query configuration.

- `Composable` — a specification with only `Where` and, for EF Core, `Include` or `ThenInclude`.
- `Configured` — a specification after `OrderBy`, `OrderByDesc`, `Skip`, `Take`, tracking modes, split/single-query, tags, or query-filter options.

All builder operations create **new** specifications, leaving the originals unchanged.

### LINQ

```csharp
var filter = Specification.Create<Student>()
    .Where(s => s.Age >= 18);
var sorting = Specification.Create<Student>()
    .OrderBy(s => s.Name)
    .OrderByDesc(s => s.Age);

Specification<Student, Configured> combined = filter.And(sorting);
var result = students.AsQueryable().BySpecification(combined).ToList();
```

### Custom specifications by inheritance

To define a reusable custom specification, derive from `Specification<T, Composable>`
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

public sealed class CustomStudentSpecification : Specification<Student, Composable>
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

`Where`, `Include`, and `ThenInclude` preserve `Composable`. Other query options return `Configured`.

`And` / `Or` combine `Where` expressions and compatible typed `Include` plans from **both** specifications (with duplicates and covered prefixes removed). Compatible query configurations are merged — for example, sorting from one operand and `Take` from the other.

Conflicts are rejected with `InvalidOperationException`: both operands specifying ordering, both specifying `Skip`, both specifying `Take`, incompatible tracking/split-query modes, or conflicting filtered Includes. Tags are appended as separate SQL comments in left-to-right order (including duplicates); ignore-filter options are combined.

The merged configuration applies to the **final combined query**, not separately to each branch of `Or`.

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

Tracking (default/no-tracking/identity resolution) and splitting (default/single/split) have one internal state each. Within a specification, the last setter wins. Across `And`/`Or`, different explicit settings conflict; default does not override the `DbContext` configuration.

`TagWith("A").TagWith("B")` generates separate ordered SQL comments. Duplicate tags remain in immutable specification state (EF Core 10 may deduplicate identical SQL comments), but empty/whitespace-only strings are ignored (the specification still becomes `Configured`). `TagWith(null)` throws. `TagWithCallSite("A")` adds one user tag plus one call-site tag with caller method, file and line; the file path may be absolute and is visible in SQL.

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

## Supported operations

| Query operation | LINQ | EF Core |
|---|---|---|
| `Where`, `And`, `Or` | Yes | Yes |
| `Select` (terminal projection) | Yes | Yes |
| `OrderBy`, `OrderByDesc` | Yes | Yes |
| `Skip`, `Take` | Yes | Yes |
| `Include`, `ThenInclude` | — | Yes |
| `AsNoTracking`, `AsNoTrackingWithIdentityResolution` | — | Yes |
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
