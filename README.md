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

### 1. Classical Specification

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

### 2. EntityFrameworkCore Support

The EntityFrameworkCore support extends the classical specification with
EF-specific operations, such as Include and ThenInclude,
to eagerly load related entities in the query.

#### Usage Example:

```csharp
var specification = EFSpecification<Student>
    .Create()
    .Where(s => s.Age < 21)
    .Include(s => s.School.MainBook.Author);
```

### Executing Specifications

To execute the specifications, you can use extension methods on
the IQueryable<TEntity> provided by EntityFrameworkCore.

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
