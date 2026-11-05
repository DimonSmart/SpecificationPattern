# IQueryable EF Core specification support

## Goal

Support both `dbContext.BySpecification(spec)` and
`dbContext.Set<TEntity>().Where(...).BySpecification(spec)` without losing
the original query or creating another evaluator.

## API contracts

- New `IQueryable<TEntity>.BySpecification(EFCoreSpecification<TEntity,TState>)`
  returns `IQueryable<TEntity>`.
- New `IQueryable<TEntity>.BySpecification(EFCoreProjectedSpecification<TEntity,TResult>)`
  returns `IQueryable<TResult>`.
- Existing `DbContext` overloads remain source compatible and delegate through
  `Set<TEntity>()` to the new single evaluation path.
- Both forms apply query options (typed Includes, tracking, ignore query filters,
  split/single query, SQL tags), then core Where/SeekAfter/ordering/paging and
  finally projection. They retain all upstream IQueryable operators.
- Input must have an EF Core-compatible asynchronous query provider; reject
  ordinary `EnumerableQuery` instead of silently ignoring EF-specific options.
- Preserve null-argument checks, immutability, deferred SQL execution,
  composable/configured state, and existing Include/Select conflict validation.
- Do not introduce repository abstractions or new runtime versions.

## Acceptance

- The queryable and context overloads compile and return the correct generic
  `IQueryable` types for entity and projected specifications.
- SQLite integration tests verify upstream predicates, ordering, paging, tags,
  navigation loading, named query filters, tracking and `SeekAfter`.
- Null sources and specifications fail promptly; LINQ-to-Objects `IQueryable`
  inputs are rejected.
- README shows source-preserving LINQ and terminal projection examples which
  are compiled and checked by CI.
- The complete existing .NET 10 solution tests pass.
