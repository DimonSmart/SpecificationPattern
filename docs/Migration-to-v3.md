# Migration to v3 — Composable LINQ Query Pipeline

> Historical migration instructions for v3. In v4, `SpecificationData`,
> `ISpecificationData<T>` and `GetWhereExpression()` were removed.
> See [Migration to v4](Migration-to-v4.md).

Both packages target .NET 10 / EF Core 10 and move from 2.x to **3.0.0** because evaluation semantics change.

## Ordered execution replaces category-based execution

All `Where`, `OrderBy`, `ThenBy`, `Skip`, `Take`, `Select` and `SelectMany`
calls run in their exact fluent order. Repeat operations as needed; they are no
longer collapsed into one final filter or paging setting.

```csharp
// v2: filters effectively moved before paging.
// v3: the predicate applies only to the selected first ten rows.
var spec = Specification.Create<Student>()
    .OrderBy(s => s.Id).Take(10).Where(s => s.Age >= 18);
```

Change the method order where the old global-filter/global-paging behavior
was intentional. `Take(10).Skip(5)` is no longer the same as `Skip(5).Take(10)`.

## Ordering keys

A second `OrderBy` now starts a **new primary ordering block**. Multi-key
sorting must use `ThenBy`/`ThenByDesc`:

```csharp
// v2
.OrderBy(s => s.Name).OrderByDesc(s => s.Age)
// v3
.OrderBy(s => s.Name).ThenByDesc(s => s.Age)
```

`ThenBy` requires a directly preceding `OrderBy` or `ThenBy` in the same
typed stage. `Where`, `Skip`, `Take` and type-changing operations break
this chain. Disabled conditional operators do not.

## Nonterminal projections

`ProjectedSpecification<TSource,TCurrent>` and its EF counterpart are
continuable. `Select`, `SelectMany(collectionSelector)`, and
`SelectMany(collectionSelector,resultSelector)` create a new typed stage.
Further filters, sorting, pagination, seek, and projections operate on
`TCurrent`. Result types can be classes, records, anonymous objects, strings
or value types. EF options are applied to the source query root and cannot be
changed from a projected specification.

```csharp
var spec = EFCoreSpecification.Create<Student>()
    .SelectMany(s => s.Books)
    .Where(book => book.Name.StartsWith("A"))
    .OrderBy(book => book.Name)
    .Take(10);
var books = db.Students.BySpecification(spec);
```

Explicit EF `Include`/`ThenInclude` conflicts with *any* `Select` or
`SelectMany`; navigate directly through selector expressions.

## Keyset pagination

`SeekAfter` requires an immediately preceding ordering block in its own
stage, with all cursor values matching the exact key types. It filters the
**input of the current sorting block** before the block is evaluated, so
`OrderBy(x => x.Id).SeekAfter(5).Take(10)` retains the intended keyset
semantics. A previous `Take` in the same stage and any same-stage `Skip`
conflict with `SeekAfter`. `Select` or `SelectMany` begins a fresh stage
whose ordering and cursor are independent.

Supported keys remain byte, short, int, long, decimal, DateTime and DateOnly.

## Composition and diagnostics

`And`/`Or` and `EnsureCompatibleWith` accept only canonical *source-stage*
specifications: initial `Where*`, followed by optional
`OrderBy/ThenBy*`, optional `SeekAfter` or `Skip`, then optional `Take`.
They reject late `Where`, multiple ordering blocks, repeated paging and
conflicting cursor/configurations immediately. `A OR true` still yields
`true` for its predicate while retaining compatible options. Projected
specifications do not offer `And`/`Or`.

In v3, `PipelineSteps` provided immutable diagnostics across typed stages,
operation order and original expression trees. The legacy `SpecificationData`
and `GetWhereExpression()` APIs were still available for canonical queries,
but were removed in v4.

## Criteria-only

`BySpecificationCriteria` on ordinary and projected specifications
removes the specification's sorting, limits, cursor and loading options, but
retains every `Where`, `Select` and `SelectMany`, including filters after
original paging. It preserves the incoming query's existing prefix. For EF
Core it retains the specified global-query-filter overrides.

All `IQueryable`, core `IEnumerable`, and EF Core `DbContext` overloads
are available for projected specifications. No intermediate materialization
is introduced by the pipeline.
