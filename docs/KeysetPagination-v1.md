# Keyset Pagination v1 — SpecificationPattern

## Goal
Provide forward-only keyset pagination over existing immutable LINQ and EF Core specifications.
The implementation reuses typed `OrderClause<TEntity, TKey>` expressions, performs no materialization,
and preserves immutable canonical `And`/`Or` semantics. The pipeline requires consecutive `OrderBy`/`ThenBy` calls and disallows `Take` before `SeekAfter`.

## Public API
```csharp
var page = EFCoreSpecification.Create<Order>()
    .Where(x => x.IsActive)
    .OrderByDesc(x => x.CreatedAt)
    .ThenBy(x => x.Id)
    .SeekAfter((lastCreatedAt, lastId))
    .Take(20);
```

- `SeekAfter(value)` accepts a single key value; `SeekAfter((a, b, ...))` accepts
  typed tuples with 2–7 elements, one per ordering clause. Types and arity are
  validated when creating the specification.
- Cursor values **exclude** the row at the boundary. Ascending keys use `>`;
  descending keys use `<`. Ties are resolved lexicographically across every key.
- At least one `OrderBy`/`OrderByDesc` is required before `SeekAfter`.
- The caller must supply a **unique ordering** (typically finish with a unique Id);
  the library does not infer uniqueness or database indexes.

## v1 boundaries
- Supported keys: `byte`, `short`, `int`, `long`, `decimal`, `DateTime`, `DateOnly`.
  No string, Guid, nullable, object-boxed, or user-defined comparator keys in v1.
- No reverse pagination, cursor serialization, keyset token generation, or provider-specific SQL.
- No `Skip` with `SeekAfter`, in either call order or across composition.
- No additional ordering or second `SeekAfter` after a cursor is configured.
- Two independently configured cursors cannot be combined.
- `Take` after `SeekAfter` and continuable `Select` remain compatible.
  `And`/`Or` merge normal filters; a single cursor filters the **entire**
  resulting expression, not one branch of `Or`.
- Keyset filtering precedes sorting, `Take`, and projection when applying a specification.
- Cursor key types are checked at runtime: the existing two-state API does not encode
  the ordering key sequence at the C# type level.

## Correctness and performance
- Preserve original selector expression trees and typed cursor values (no `object[]`,
  `ITuple` extraction, or cast back from `object`); create comparisons without
  `Expression.Compile`, LINQ-to-Objects fallback, or intermediate materialization.
- Capture boundary values for EF Core parameter extraction rather than embedding
  constant SQL values.
- Query-provider SQL translation, collations and index selection are provider-specific;
  integration tests must exercise SQLite translation and result ordering.
- Concurrent inserts/deletes are not a snapshot guarantee; consumers requiring a
  stable snapshot must provide transaction/isolation guarantees.
- Use a composite index consistent with the query filters and ordering for performance.

## Acceptance criteria
1. Single-key and multi-key forward pagination, mixed ASC/DESC, ties and successive pages.
2. Deferred `IQueryable` execution; EF Core SQLite SQL translation, no client filtering.
3. Validation for missing order, wrong arity or type, unsupported keys, `Skip`, repeated
   cursors and ordering changes.
4. Correct `And`/`Or` semantics, including `Where`, `Take`, and `Select`.
5. Both packages compile on .NET 10; the API-reference example is checked against
   compiled source and the GitHub Actions test workflow passes.
