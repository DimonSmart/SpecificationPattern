# IDD Intent Index

This index helps humans and Coding Agents find relevant current intent documents.
It is not the source of truth.

Current `IDD-NNNN` documents directly under `.idd/intent/` contain normative
product intent, ADRs, or active spikes.

`GLOSSARY.md`, when present, is an optional unnumbered vocabulary support file
and is not listed in this index.

Git history is the source for deleted or previous document versions.

For shared or cross-cutting product intent, use `Area`, `Notes`, or equivalent
discovery metadata to make the scope obvious to planners. Existing projects do
not need a schema migration; keep their current INDEX shape when it already
communicates this information.

Implementation-only durable constraints belong in the optional
`.idd/engineering/` layer, not in this index.

## Current documents

The `Document` column contains stable `IDD-NNNN` identifiers only. Do not put
filenames, file paths, or Markdown links in this column. Resolve an identifier to
the unique current `.idd/intent/IDD-NNNN.*.md` file when the document must be
opened.

| Document | Role | Area | Notes |
| --- | --- | --- | --- |
| IDD-0001 | spec | Immutable composition | Read-only contracts, immutable configuration, And/Or semantics and validation |
| IDD-0002 | spec | Typed LINQ pipeline | Ordered operations, typed sorting, conditional configuration and diagnostics |
| IDD-0003 | spec | Projections and criteria | Typed Select/SelectMany, reusable projections, criteria-only evaluation |
| IDD-0004 | spec | EF Core query execution | IQueryable/DbContext, provider, tracking, query filters, Search and tags |
| IDD-0005 | spec | EF Core Includes | Typed/string navigation paths, filtered Include, merging and conflicts |
| IDD-0006 | spec | Forward keyset pagination | SeekAfter cursors, boundaries, runtime rules and provider considerations |
| IDD-0007 | spec | Packaging and compatibility | NuGet packages, supported frameworks and coordinated releases |
