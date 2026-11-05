# Migration to v2.0

Version 2 removes the type-state API. Both NuGet packages use immutable single-state specifications.
This change breaks existing generic signatures at source and binary levels.

| v1 API | v2 API |
| --- | --- |
| `Specification<T, Composable>` | `Specification<T>` |
| `Specification<T, Configured>` | `Specification<T>` |
| `EFCoreSpecification<T, Composable>` | `EFCoreSpecification<T>` |
| `EFCoreSpecification<T, Configured>` | `EFCoreSpecification<T>` |
| Extension methods with `TState` | Extension methods without `TState` |
| Type transitions after query configuration | No type transitions |

Update explicitly declared variables, inheriting specifications, helper signatures
and generic method type arguments. The `Create<T>()` factory signatures are unchanged.

```csharp
var spec = Specification.Create<Student>();
if (sortByName)
    spec = spec.OrderBy(s => s.Name);
if (pageSize > 0)
    spec = spec.Take(pageSize);
```

All fluent operations remain immutable. Disabled conditional operations return
the original instance. `And` and `Or` preserve their filter semantics and
immediately reject incompatible settings. `EnsureCompatibleWith` checks
compatibility without creating a combined specification.

`Include` and `ThenInclude` retain typed expressions; `Select` still yields
a terminal projection. No obsolete generic adapters are supplied.
