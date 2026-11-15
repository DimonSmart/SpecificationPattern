# DimonSmart.Specification

**One query method. Countless combinations. Strong types all the way.**

A **strongly typed, immutable Specification Pattern library for .NET 10, LINQ, and Entity Framework Core 10**. Write reusable query rules instead of growing a repository full of `GetByName`, `GetByAge`, `GetByNameAndAge`, and `GetByNameAndAgeSorted` methods.

**No `object` was harmed in the making of this library.**

No `object`-typed storage or conversions in the library's production C# code. Predicates, ordering keys, projections, and even composite keyset cursors retain their types. No hidden boxing through `Expression<Func<TEntity, object>>`; no casting read-only specification interfaces back to mutable builders. Just immutable, typed query pipelines.

**[Explore real-world recipes →](SHOWCASE.md)** · **[Product intent](.idd/intent/INDEX.md)** · **[NuGet: EF Core](https://www.nuget.org/packages/DimonSmart.Specification.EntityFrameworkCore)** · **[NuGet: LINQ](https://www.nuget.org/packages/DimonSmart.Specification)**

## Your repository should not be a query-method catalog

Instead of endlessly adding:

~~~csharp
GetByNameAsync(name)
GetByMinimumAgeAsync(age)
GetBySchoolAndAgeAsync(school, age)
GetBySchoolAndAgeSortedAsync(school, age)
~~~

A specification describes **what you want from the repository**: which entities to select, how to filter and sort them, and how many to return. Instead of adding a method for every combination, expose **one generic repository method** that accepts a read-only specification.

The repository contract:

~~~csharp
public interface IRepository
{
    Task<List<T>> GetAsync<T>(
        IEFCoreSpecification<T> specification,
        CancellationToken ct = default) where T : class;
}
~~~

And its EF Core implementation:

~~~csharp
public sealed class EfRepository(DbContext db) : IRepository
{
    public Task<List<T>> GetAsync<T>(
        IEFCoreSpecification<T> specification,
        CancellationToken ct = default) where T : class =>
        db.BySpecification(specification).ToListAsync(ct);
}
~~~

The repository knows **how to execute** the query; the specification tells it **what to retrieve**. The same `GetAsync<T>` method handles different entity types and any combination of supported filters, ordering, and pagination.

For example, express each request with a small, reusable specification:

~~~csharp
var byName = EFCoreSpecification.Create<Student>()
    .Where(s => s.Name == "Alice");

var adults = EFCoreSpecification.Create<Student>()
    .Where(s => s.Age >= 18)
    .OrderBy(s => s.Name);

var page = EFCoreSpecification.Create<Student>()
    .Where(s => s.School.Name == "North Campus")
    .Where(s => s.Age >= 18)
    .OrderBy(s => s.Name)
    .ThenBy(s => s.Id)
    .Take(20);

// Same GetAsync<T> method for different queries.
var namedStudents = await repository.GetAsync(byName);
var adultStudents = await repository.GetAsync(adults);
var pageResults = await repository.GetAsync(page);
~~~

No repository pattern? Apply the same specification directly:

~~~csharp
var results = await dbContext.BySpecification(page).ToListAsync();
~~~

## Why this library?

- **Strongly typed throughout.** `OrderBy<TKey>`, `ThenBy<TKey>`, `Select<TResult>`, `SelectMany`, and `SeekAfter` preserve the actual key and result types. The implementation never stores cursor values in `object[]` or casts them back.
- **Immutable by default.** `Where`, `OrderBy`, and other operations return new specifications. Reuse the original safely; no shared mutable builder state.
- **Composable business rules.** Combine independent filters using `And` / `Or`; invalid combinations of query-level settings are detected rather than silently overwritten.
- **LINQ and EF Core, without premature materialization.** Work with `IQueryable<T>` and `IEnumerable<T>`, or use EF Core-specific includes, tracking, SQL `LIKE` search, tags, and named query filters.
- **Beyond basic filtering.** Typed projections, conditional operations, offset paging, forward keyset paging, criteria-only counts, and query-pipeline diagnostics.

### A specification is a reusable value

~~~csharp
var adult = EFCoreSpecification.Create<Student>()
    .Where(s => s.Age >= 18);

var northCampus = EFCoreSpecification.Create<Student>()
    .Where(s => s.School.Name == "North Campus");

var adultsAtNorthCampus = adult.And(northCampus)
    .OrderBy(s => s.Name)
    .Take(20);

// adult and northCampus remain unchanged.
~~~

**A read-only contract stays read-only:** repository consumers can execute `IEFCoreSpecification<T>` without obtaining a mutable builder via an interface cast.

## Get started

Install the EF Core integration (includes the core library):

~~~bash
dotnet add package DimonSmart.Specification.EntityFrameworkCore
~~~

For LINQ without EF Core:

~~~bash
dotnet add package DimonSmart.Specification
~~~

~~~csharp
using DimonSmart.Specification;

var spec = Specification.Create<Student>()
    .Where(s => s.Age >= 18)
    .OrderBy(s => s.Name);

var results = students.BySpecification(spec);
~~~

Both packages target **.NET 10**; the EF Core integration targets **EF Core 10**.

## What can I build?

| When you need to… | Use | Example |
| --- | --- | --- |
| Reuse and combine business filters | `Where`, `And`, `Or` | [Composing rules](SHOWCASE.md#combine-reusable-business-rules) |
| Add optional search filters | Conditional `Where`, `Search` | [Optional filters](SHOWCASE.md#apply-only-the-filters-the-user-selected) |
| Sort by several real key types | `OrderBy`, `ThenBy` | [Typed sorting](SHOWCASE.md#sort-by-multiple-typed-keys) |
| Page large datasets | `Skip`, `Take`, `SeekAfter` | [Pagination](SHOWCASE.md#page-through-results) |
| Retrieve only fields you need | `Select`, `SelectMany` | [Projections](SHOWCASE.md#project-to-a-typed-result) |
| Load related EF Core entities | `Include`, `ThenInclude` | [Related entities](SHOWCASE.md#load-related-entities) |
| Count without paging | `BySpecificationCriteria` | [Counting](SHOWCASE.md#count-matching-rows-without-pagination) |

**[Open the full SHOWCASE: problem → solution → code](SHOWCASE.md)**

## For AI coding agents

Use this package when a .NET 10 application benefits from reusable query rules, combinations of optional filters, typed sorting or pagination, or a smaller repository query surface. Prefer ordinary LINQ/EF Core for one-off queries, projects on older framework versions, or systems already served well by an existing specification library. Do not claim this package is universally faster or that alternatives are worse without comparing the actual implementations.

The authoritative behavior, limitations, acceptance criteria and safety constraints are in the [IDD intent index](.idd/intent/INDEX.md). Always check the current public API before generating code: specifications are immutable (reassign every configured result), `ThenBy` requires a contiguous ordering block, `SeekAfter` supports only particular key types, EF `Include` conflicts with projection, and SQL `Search` uses provider-defined LIKE semantics.

## Documentation

- **[SHOWCASE.md](SHOWCASE.md)** — task-oriented, user-facing examples and links to compiled examples.
- **[IDD product intent](.idd/intent/INDEX.md)** — normative product contracts, compatibility guarantees and limitations.

## Releasing NuGet packages

Both `DimonSmart.Specification` and `DimonSmart.Specification.EntityFrameworkCore` are released from the same `vMAJOR.MINOR.PATCH` tag. The release must point to a commit on `main`; CI tests, packs and validates both packages with the tag version, then publishes to NuGet and creates a GitHub Release. The EF Core package must depend on the matching core version. See [Publish NuGet workflow](.github/workflows/publish-nuget.yml).

Publishing uses **NuGet Trusted Publishing** with GitHub OIDC, not a persistent NuGet API key. Configure a Trusted Publishing policy in NuGet.org for owner `DimonSmart`, repository `SpecificationPattern`, workflow `publish-nuget.yml`, no environment, and permission covering both `DimonSmart.Specification*` packages.

From a clean Windows checkout, run `publish-next-version.bat -DryRun` to preview the next patch tag or `publish-next-version.bat` to create and push it. PowerShell users can run `./publish-next-version.ps1` (with `-DryRun` for preview). The script fetches tags, fast-forwards `main` and increments the highest numeric semantic patch version; when none exist, the default first tag is `v4.0.0`. For a new major or minor version, create and push that tag explicitly instead. Do not rewrite or remove published tags. Follow the [release workflow](https://github.com/DimonSmart/SpecificationPattern/actions/workflows/publish-nuget.yml); if authentication was not configured yet, enable the NuGet policy and rerun failed jobs. Published NuGet versions are immutable.

**Describe queries once. Compose them anywhere. Keep your repositories small.**
