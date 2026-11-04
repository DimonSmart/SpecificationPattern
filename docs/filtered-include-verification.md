# Filtered Include verification matrix

Baseline: `a36aa59d5c910b86e1254256187c6f865008a4ba` (.NET 10 / EF Core 10).
Scope: typed Include plans, compositional conflicts, SQLite integration.
All verification tests use the existing EF Core test projects and their IncludeBlog / Student models.

## Unit / API coverage

| ID | Existing coverage before audit | Added coverage | Notes |
| --- | --- | --- | --- |
| U01 | `TypedThenIncludeKeepsElementTypeAndState` | `AllSevenFilteredIncludeOperatorsPreserveTypedPaths` | Both sort directions and secondary order directions; typing of chained operators |
| U02 | `TypedThenIncludeKeepsElementTypeAndState` | `NestedFilteredThenIncludeMatchesDirectEfCore` | Compile-time typing and nested-filter execution are separate checks |
| U03 | `DirectReferenceChainAndPrefixDeduplication`; filtered `Includes` assertions | `AllSevenFilteredIncludeOperatorsPreserveTypedPaths` | Diagnostic paths identify the navigation, not LINQ methods |
| U04 | `UnsupportedOperatorFailsBeforeQueryExecution` | `UnsupportedCollectionOperatorsAndInvalidPathsFailEarly` | `Select`, `Distinct` and invalid member paths |
| U05 | `DistinctFiltersOnSameNavigationConflictDuringAndOrAndInclude` | — | Independently constructed identical filters are intentionally incompatible |
| U06 | `FilteredAndUnfilteredIncludeConflictImmediately` | — | Existing strict policy; path and reason checked |
| U07 | `BranchesFromSameFilteredSourceShareFilterIdentity` | `FilteredIncludeBranchingAndCompositionPreserveOriginalState`; `SharedFilteredBranchesSurviveAndOrAndFurtherThenInclude` | Both composition operators and sibling branches |
| U08 | `DirectReferenceChainAndPrefixDeduplication`; `IndependentIncludesDeduplicateLongerAndShorterPaths` | `AbsorbedShortIncludeCanBeExtendedWithoutDroppingExistingBranch` | Includes absorbed active-prefix case |
| U09 | `FilterConflictOnSharedIntermediateSegmentDetected` | — | Early conflict on a shared collection ancestor |
| U10 | `BranchesFromSameFilteredSourceShareFilterIdentity` | `FilteredIncludeBranchingAndCompositionPreserveOriginalState`; `SharedFilteredBranchesSurviveAndOrAndFurtherThenInclude` | Source and branches remain unchanged |
| U11 | `TypedThenIncludeKeepsElementTypeAndState` | `AllSevenFilteredIncludeOperatorsPreserveTypedPaths` | `Composable` vs `Configured`, including `AsNoTracking` |

## SQLite integration coverage

| ID | Existing coverage before audit | Added coverage | Notes |
| --- | --- | --- | --- |
| I01 | `FilteredIncludeExecutesWhereOrderByThenBySkipTake` | `RootWhereIsIndependentOfFilteredInclude` | Filtered children without dropping roots |
| I02 | `FilteredIncludeExecutesWhereOrderByThenBySkipTake`; `FilteredIncludeSupportsDescendingAndThenByDescending` | `FilteredPaginationIsPerParentAndPreservesParentsWithNoMatches` | Compare against direct EF Core |
| I03 | `FilteredIncludeExecutesWhereOrderByThenBySkipTake`; `FilteredIncludeSupportsDescendingAndThenByDescending` | `FilteredPaginationIsPerParentAndPreservesParentsWithNoMatches` | Stable secondary ordering with unique Id |
| I04 | `FilteredIncludeExecutesWhereOrderByThenBySkipTake` | `StandaloneSkipAndTakeMatchDirectEfCorePerParent`; `FilteredPaginationIsPerParentAndPreservesParentsWithNoMatches` | Separate Skip / Take and combined operations |
| I05 | `FilteredIncludeExecutesWhereOrderByThenBySkipTake` | `FilteredPaginationIsPerParentAndPreservesParentsWithNoMatches` | Preserves original filter/order/page chain |
| I06 | None (one seeded parent) | `FilteredPaginationIsPerParentAndPreservesParentsWithNoMatches` | Large, Small, Empty; check exact child keys per parent |
| I07 | None (one seeded parent) | `FilteredPaginationIsPerParentAndPreservesParentsWithNoMatches` | Parent with zero matches is retained |
| I08 | `FilteredParentBranchingSharesDescriptorAndPreservesBothChildren` | `SharedFilteredBranchesSurviveAndOrAndFurtherThenInclude` | Include + ThenInclude without pagination |
| I09 | `FilteredCollectionFollowingThenIncludeIsApplied` | `NestedFilteredThenIncludeMatchesDirectEfCore` | Direct EF Core parity and nested child keys |
| I10 | None | `FilteredParentWithNestedThenIncludeHasProviderParity` | Positive parity if translated, otherwise verify SQLite APPLY exception parity |
| I11 | `CombiningIndependentBranchesAndOrKeepsAllIncludes` | `AbsorbedShortIncludeCanBeExtendedWithoutDroppingExistingBranch` | Both branches survive normalization |
| I12 | `FilteredParentBranchingSharesDescriptorAndPreservesBothChildren` | `SharedFilteredBranchesSurviveAndOrAndFurtherThenInclude` | Shared filter source with And, Or and deeper path |
| I13 | `CombiningIndependentBranchesAndOrKeepsAllIncludes` | `RootWhereIsIndependentOfFilteredInclude` | Root Where is separate from filtered navigation |
| I14 | Immutable checks of metadata, no cross-context reuse | `ImmutableSpecificationCanBeReusedAcrossContextsAndSplitQueries` | One specification, two independent contexts |
| I15 | `FilteredCollectionFollowingThenIncludeIsApplied` (no tracking) | `ImmutableSpecificationCanBeReusedAcrossContextsAndSplitQueries` | `AsNoTracking` + `AsSplitQuery` parity |

## Execution contract and limitations

- The comparison oracle is **direct EF Core on the same SQLite version**, with independently seeded, clean contexts and equivalent tracking/splitting modes.
- Compare root keys and included entity keys, using stable ordering for pagination, rather than SQL text/row order.
- A successful C# compilation only proves API typing. In particular, SQLite can reject filtered pagination followed by `ThenInclude` where EF generates `APPLY`. The conditional regression test requires the specification query to have the **same result or provider failure** as the direct query; it never treats a failed SQL query as successful.
- `FilterId` identifies the source expression, not its textual shape. Strict conflicts are deliberate and remain stricter than EF Core's direct Include compatibility in some scenarios.
- Runtime query execution uses typed Include plans, not diagnostic `EFCoreSpecificationData.Includes` string paths.
- No SQL provider or custom client-side materialization was added. No production implementation change is intended unless tests reproduce a defect.

## Verification

Run:

```bash
python3 scripts/check_documentation_examples.py
dotnet restore DimonSmart.Specification.sln
dotnet test DimonSmart.Specification.sln --no-restore --configuration Release
```

The same commands are executed by `.github/workflows/dotnet.yml` on pushes to `main`.
