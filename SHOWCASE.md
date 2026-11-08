# DimonSmart.Specification — SHOWCASE

**Have a query problem? Find the scenario, copy the pattern, and keep your repository API small.**

These recipes use the actual `DimonSmart.Specification` and `DimonSmart.Specification.EntityFrameworkCore` APIs for .NET 10 / EF Core 10. They're intentionally short; the [API reference](docs/REFERENCE.md) explains every option and constraint.

**Example model:** `Student` has `Id` (int), `Name` (string), `Age` (int), `School` (navigation with `Name` and `MainBook`), and `Books` (collection). The EF Core examples assume a `DbContext dbContext` with a `DbSet<Student>`. Async examples require `Microsoft.EntityFrameworkCore`.

~~~csharp
using DimonSmart.Specification;
using DimonSmart.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
~~~

## Stop adding repository methods for every query

**I need:** `GetByName`, `GetByAge`, `GetBySchoolAndAge`, and their future combinations, without a new method for each one.

**Do this:** expose one method accepting a *read-only* specification.

~~~csharp
public interface IStudentRepository
{
    Task<List<Student>> ListAsync(
        IEFCoreSpecification<Student> specification,
        CancellationToken cancellationToken = default);
}

public sealed class StudentRepository(DbContext db) : IStudentRepository
{
    public Task<List<Student>> ListAsync(
        IEFCoreSpecification<Student> specification,
        CancellationToken cancellationToken = default) =>
        db.BySpecification(specification).ToListAsync(cancellationToken);
}
~~~

Call that same method with completely different query rules:

~~~csharp
var byName = EFCoreSpecification.Create<Student>()
    .Where(s => s.Name == "Alice");

var olderStudents = EFCoreSpecification.Create<Student>()
    .Where(s => s.Age >= 18)
    .OrderByDesc(s => s.Age);

var atNorthCampus = EFCoreSpecification.Create<Student>()
    .Where(s => s.School.Name == "North Campus")
    .OrderBy(s => s.Name)
    .Take(20);

var result = await repository.ListAsync(atNorthCampus);
~~~

**No repository required:** `await dbContext.BySpecification(atNorthCampus).ToListAsync()` applies the same specification directly.

## Combine reusable business rules

**I need:** rules that can be reused independently and combined without mutating the originals.

~~~csharp
var adult = EFCoreSpecification.Create<Student>()
    .Where(s => s.Age >= 18);

var northCampus = EFCoreSpecification.Create<Student>()
    .Where(s => s.School.Name == "North Campus");

var adultsAtNorth = adult.And(northCampus);
var adultOrNorth = adult.Or(northCampus);

var result = await dbContext.BySpecification(
    adultsAtNorth.OrderBy(s => s.Id)).ToListAsync();
~~~

`adult` and `northCampus` remain unchanged. Prefer composing filter-only specifications, then applying ordering and paging. If both sides configure incompatible ordering or pagination, composition rejects the conflict instead of arbitrarily choosing one side.

## Apply only the filters the user selected

**I need:** an optional name, minimum age, or other filter without writing a different query for each combination.

~~~csharp
string? name = request.Name;
int minimumAge = request.MinimumAge;
bool filterByAge = request.HasMinimumAge;

var spec = EFCoreSpecification.Create<Student>()
    .Where(s => s.Name == name, !string.IsNullOrWhiteSpace(name))
    .Where(s => s.Age >= minimumAge, filterByAge)
    .OrderBy(s => s.Id);

var result = await dbContext.BySpecification(spec).ToListAsync();
~~~

The `bool condition` overload is available for `Where`, sorting, `Skip`, `Take`, and many EF Core-specific options. A disabled operation is not appended.

For dynamic query-building, remember **immutability**:

~~~csharp
var spec = EFCoreSpecification.Create<Student>();

if (request.HasMinimumAge)
    spec = spec.Where(s => s.Age >= request.MinimumAge);

if (!string.IsNullOrWhiteSpace(request.Name))
    spec = spec.Where(s => s.Name == request.Name);
~~~

Calling `spec.Where(...)` without assigning its result does not change `spec`.

## Sort by multiple typed keys

**I need:** sort students by decreasing age, then by name and ID.

~~~csharp
var spec = EFCoreSpecification.Create<Student>()
    .OrderByDesc(s => s.Age) // int
    .ThenBy(s => s.Name)     // string
    .ThenBy(s => s.Id);      // int
~~~

Each selector keeps its actual `TKey`: the library does **not** turn numeric and date keys into `Expression<Func<Student, object>>`. `ThenBy` requires an active ordering block.

## Page through results

**I need:** a simple page number and page size.

~~~csharp
int pageNumber = 3;
int pageSize = 20;

var spec = EFCoreSpecification.Create<Student>()
    .OrderBy(s => s.Id)
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize);

var page = await dbContext.BySpecification(spec).ToListAsync();
~~~

**I need:** forward **keyset pagination** (seek from the last row rather than using an offset).

~~~csharp
// Values obtained from the last row of the previous page.
int lastAge = 22;
int lastId = 105;

var nextPageSpec = EFCoreSpecification.Create<Student>()
    .OrderByDesc(s => s.Age)
    .ThenBy(s => s.Id)
    .SeekAfter((lastAge, lastId))
    .Take(20);

var nextPage = await dbContext.BySpecification(nextPageSpec).ToListAsync();
~~~

`SeekAfter` keeps each cursor key typed; no `object[]`, `ITuple` unpacking, or boxed cursor storage. It supports **1–7 keys**, currently limited to non-nullable `byte`, `short`, `int`, `long`, `decimal`, `DateTime`, and `DateOnly` keys. The cursor must match the sorting keys in order, number, and type; the combination is validated at runtime. For reliable paging, use a unique, deterministic ordering (here `Id` is the tie-breaker). See [keyset pagination details](docs/KeysetPagination-v1.md).

## Project to a typed result

**I need:** only a few columns, without loading the full entity.

~~~csharp
var spec = EFCoreSpecification.Create<Student>()
    .Where(s => s.Age >= 18)
    .OrderBy(s => s.Id)
    .Select(s => new { s.Id, s.Name, s.Age })
    .Take(20);

var summaries = await dbContext.BySpecification(spec).ToListAsync();
~~~

The specification's result type follows the projection. You can also operate *after* the projection:

~~~csharp
var spec = EFCoreSpecification.Create<Student>()
    .Select(s => new { s.Id, s.Age })
    .Where(s => s.Age >= 18)
    .OrderBy(s => s.Id)
    .Select(s => s.Id);
~~~

`SelectMany` is supported for collection projections. EF Core SQL translation still depends on the provider.

## Reuse a projection

**I need:** keep a DTO projection independent from the filtering rules.

~~~csharp
var summary = EFCoreSpecification.Create<Student>()
    .Select(s => new { s.Id, s.Name });

var adults = EFCoreSpecification.Create<Student>()
    .Where(s => s.Age >= 18);

var projected = adults.WithProjectionOf(summary);
var rows = await dbContext.BySpecification(projected).ToListAsync();
~~~

Both the filter and the projection are reusable values.

## Load related entities

**I need:** load `School` and its `MainBook` while keeping the specification reusable.

~~~csharp
var spec = EFCoreSpecification.Create<Student>()
    .Where(s => s.Age >= 18)
    .Include(s => s.School)
    .ThenInclude(s => s.MainBook)
    .AsNoTracking();

var result = await dbContext.BySpecification(spec).ToListAsync();
~~~

Use `Include` / `ThenInclude` for entity loading; **don't combine explicit includes with `Select`/`SelectMany`**. For DTO projections, access related properties directly in the selector instead.

## Search using database SQL LIKE

**I need:** prefix or wildcard matching provided by the database, not an in-memory string search.

~~~csharp
var spec = EFCoreSpecification.Create<Student>()
    .Search(s => s.Name, "Ali%")
    .OrderBy(s => s.Id);

var result = await dbContext.BySpecification(spec).ToListAsync();
~~~

`Search` uses EF Core's SQL `LIKE` translation. The meaning of `%`, `_`, collation, and escaping follows the database provider. Escape user-entered wildcard characters if they should be treated literally.

## Count matching rows without pagination

**I need:** get the number of *all* matching students while fetching only one page of results.

~~~csharp
var spec = EFCoreSpecification.Create<Student>()
    .Where(s => s.Age >= 18)
    .OrderBy(s => s.Id)
    .Skip(40)
    .Take(20);

var total = await dbContext.BySpecificationCriteria(spec).CountAsync();
var page = await dbContext.BySpecification(spec).ToListAsync();
~~~

`BySpecificationCriteria` applies filtering criteria and relevant global query-filter settings, **not** sorting, paging, includes, or other query-shaping options.

## Control EF Core query behavior

**I need:** no tracking, split queries for related collections, or a SQL tag to identify a query.

~~~csharp
var spec = EFCoreSpecification.Create<Student>()
    .Include(s => s.Books)
    .AsNoTracking()
    .AsSplitQuery()
    .TagWith("students-report");

var result = await dbContext.BySpecification(spec).ToListAsync();
~~~

EF Core 10 named query filters can be disabled selectively using `IgnoreQueryFilters("FilterName")`. Only use this with trusted filter names; disabling global filters is not an authorization mechanism.

## Apply to an existing IQueryable

**I need:** start from a prefiltered EF Core query without materializing it.

~~~csharp
IQueryable<Student> query = dbContext.Set<Student>()
    .Where(s => s.Age >= 16);

var spec = EFCoreSpecification.Create<Student>()
    .Where(s => s.School.Name == "North Campus")
    .OrderBy(s => s.Id);

var result = await query.BySpecification(spec).ToListAsync();
~~~

`BySpecification` composes expression trees into the existing provider query. It does not call `ToList` on the intermediate query.

## Work with in-memory collections

**I need:** use the same specification idea without EF Core.

~~~csharp
var spec = Specification.Create<Student>()
    .Where(s => s.Age >= 18)
    .OrderBy(s => s.Name)
    .Take(10);

IEnumerable<Student> results = students.BySpecification(spec);
~~~

Use `DimonSmart.Specification` for `IQueryable<T>` and `IEnumerable<T>`. To check one item against criteria only:

~~~csharp
var adult = Specification.Create<Student>()
    .Where(s => s.Age >= 18);

bool matches = adult.IsSatisfiedBy(student);
~~~

`IsSatisfiedBy` ignores sorting and pagination; predicates must support LINQ-to-Objects.

## Inspect the query pipeline

**I need:** diagnostics without exposing a mutable builder to the caller.

~~~csharp
var spec = Specification.Create<Student>()
    .Where(s => s.Age >= 18)
    .OrderBy(s => s.Name)
    .Take(10);

IReadOnlyList<QueryPipelineStep> steps = spec.PipelineSteps;
~~~

`PipelineSteps` is a read-only snapshot of the pipeline's operations, their order, and typed expression metadata. It does not control execution.

## What the strong-typing promise means

> **No `object` was harmed in the making of this library.**

In the **production C# implementation**, `object` is not used as a generic bucket for ordering, cursor values, or specification state. The library keeps selector key types, results, and cursor values typed rather than erasing and reconstituting them with casts. Public read-only specification interfaces are not cast back into mutable builders.

This is a statement about **library-generated code**, not a restriction on caller code: callers can explicitly provide a selector such as `s => (object)s.Age`, and the library does not rewrite it. Also, not every usage error can be caught by the C# compiler: `SeekAfter` validates the cursor's compatibility with the runtime ordering block when building the specification. EF Core providers can impose additional translation limits.

**Next:** [Back to README](README.md) · [Complete API reference](docs/REFERENCE.md)
