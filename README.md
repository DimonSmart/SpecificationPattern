# Specification Pattern Implementation in C#
## Overview

The Specification Pattern is a behavioral design pattern that
allows you to define reusable and composable query specifications.
It is commonly used in scenarios where you need to filter,
order, and include related entities in a query.
The main idea behind this pattern is to encapsulate the logic of a query
into separate specification classes, which can then be combined
and reused to build complex queries.

In this C# implementation of the Specification Pattern, we have two parts:

### 1. Classical Specification (Specification class)

The classical specification provides the core functionality to build query
specifications. It contains methods to define filtering criteria (Where),
   sorting (OrderBy, OrderByDesc),
   and logical operations (And, Or) to combine multiple specifications together.

#### Usage Example:

```csharp
var specification = Specification<Student>
    .Create()
    .Where(s => s.Age < 21)
    .OrderBy(s => s.Name)
    .OrderByDesc(s => s.Age);
```

### 2. Specification for use with EntityFrameworkCore (EFCoreSpecification)

The EntityFrameworkCore support extends the classical specification with
EF-specific operations, such as Include and ThenInclude,
to eagerly load related entities in the query.

#### Usage Example:

```csharp
var specification = EFCoreSpecification<Student>
    .Create()
    .Where(s => s.Age < 21)
    .Include(s => s.School.MainBook.Author);
```

### Executing Specifications

To execute the specifications, you can use extension methods on
the DBContext provided.

```csharp
var under21 = _testDBContext.BySpecification(specification).ToList();
```

### Custom Specifications

Developers can create their custom specification classes by inheriting
from the `Specification<TEntity>` class and pass parameters through
the constructor.

#### Example:

```csharp
public class CustomStudentSpecification : Specification<Student>
{
    public CustomStudentSpecification(int minAge, string schoolName)
    {
        Where(s => s.Age >= minAge && s.School.Name == schoolName);
    }
}
```

## Conclusion

The Specification Pattern in C# allows you to create reusable and composable query specifications for different data access scenarios.
By providing EntityFrameworkCore support,
you can efficiently use these specifications in Entity Framework queries,
enhancing query performance and readability.

Remember that you can also create custom specification classes
by inheriting from the base Specification class,
making the pattern even more flexible and adaptable to various business
requirements.

## Supported functions
### `Where(Expression<Func<T, bool>> expr)`
Specifies a filtering condition for the data query.

#### Example:
```csharp
var specification = EFCoreSpecification<Student>
    .Create()
    .Where(s => s.Age < 21);
```

### `OrderBy(Expression<Func<T, object>> orderByExpression)`
Specifies an ascending order for the data query.
#### Example:
```csharp
var specification = EFCoreSpecification<Student>
    .Create()
    .OrderBy(s => s.Name);
```

### `OrderByDesc(Expression<Func<T, object>> orderByExpression)`
Specifies an ascending order for the data query
#### Example:
```csharp
var specification = EFCoreSpecification<Student>
    .Create()
    .OrderByDesc(s => s.Name);
```

### `Take(int take)`
Specifies the number of elements to be skipped from the query.
#### Example:
```csharp
var specification = EFCoreSpecification<Student>
    .Create()
    .Take(10);
```

### `Skip(int skip)`
Specifies the number of elements to be skipped from the query.
#### Example:
```csharp
var specification = EFCoreSpecification<Student>
    .Create()
    .Skip(20);
```

### `Include(Expression<Func<T, TProperty>> includeExpression) / ThenInclude`
Adds an "Include" statement to the query, specifying related entities to be loaded.
#### Example:
```csharp
var specification = EFCoreSpecification<Student>
    .Create()
    .Include(s => s.Orders)
        .ThenInclude(order => order.OrderDetails);
```

### `AsNoTracking()`
[AsNoTracking](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.entityframeworkqueryableextensions.astracking))
specifies that the query should be executed with "NoTracking" behavior.

#### Example:
```csharp
var specification = EFCoreSpecification<Student>
    .Create()
    .AsNoTracking();
```

### `AsNoTrackingWithIdentityResolution()`
[AsNoTrackingWithIdentityResolution](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.entityframeworkqueryableextensions.asnotrackingwithidentityresolution))
specifies that the query should be executed with "NoTracking" and identity resolution behavior.
#### Example:
```csharp
var specification = EFCoreSpecification<Student>
    .Create()
    .AsNoTrackingWithIdentityResolution();
```

### `AsSplitQuery()`
[AsSplitQuery](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.relationalqueryableextensions.assplitquery)
specifies that the query should be executed with "SplitQuery" behavior.

#### Example:
```csharp
var specification = EFCoreSpecification<Student>
    .Create()
    .AsSplitQuery();
```

### `AsSingleQuery()`
[AsSingleQuery](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.relationalqueryableextensions.assinglequery)
Specifies that the query should be executed with "SingleQuery" behavior.
#### Example:
```csharp
var specification = EFCoreSpecification<Student>
    .Create()
    .AsSingleQuery();
```

### `IgnoreAutoIncludes()`
[IgnoreAutoIncludes](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.entityframeworkqueryableextensions.ignoreautoincludes)
specifies to ignore automatically included related entities in the query.
#### Example:
```csharp
var specification = EFCoreSpecification<Student>
    .Create()
    .IgnoreAutoIncludes();
```

### `IgnoreQueryFilters()`
[IgnoreQueryFilters](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.entityframeworkqueryableextensions.ignorequeryfilters)
specifies to ignore query filters defined in the model.
#### Example:
```csharp
var specification = EFCoreSpecification<Student>
    .Create()
    .IgnoreQueryFilters();
```

### `Or(IEFCoreSpecification<T> or)`
Combines the current specification with another specification using the logical OR operator.
#### Example:
```csharp
var youngStudentsSpec = EFCoreSpecification<Student>
    .Create()
    .Where(s => s.Age < 21);

var studentsFromNewYorkSpec = EFCoreSpecification<Student>
    .Create()
    .Where(s => s.City == "New York");

var combinedSpecificationWithOr = youngStudentsSpec.Or(studentsFromNewYorkSpec);
```

### `And(IEFCoreSpecification<T> and)`
Combines the current specification with another specification using the logical AND operator.
#### Example:
```csharp
var youngStudentsSpec = EFCoreSpecification<Student>
    .Create()
    .Where(s => s.Age < 21);

var studentsFromNewYorkSpec = EFCoreSpecification<Student>
    .Create()
    .Where(s => s.City == "New York");

var combinedSpecificationWithAnd = youngStudentsSpec.And(studentsFromNewYorkSpec);
```
