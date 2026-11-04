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

`And` / `Or` combine `Where` expressions and retain `Include` paths from **both** specifications (with duplicates removed). Compatible query configurations are merged — for example, sorting from one operand and `Take` from the other.

Conflicts are rejected with `InvalidOperationException`: both operands specifying ordering, both specifying `Skip`, both specifying `Take`, or incompatible tracking/split-query modes. Tags are appended; ignore-filter options are combined.

The merged configuration applies to the **final combined query**, not separately to each branch of `Or`.

## Supported operations

| Query operation | LINQ | EF Core |
|---|---|---|
| `Where`, `And`, `Or` | Yes | Yes |
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
