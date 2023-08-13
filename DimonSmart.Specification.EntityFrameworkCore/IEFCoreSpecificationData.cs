namespace DimonSmart.Specification.EntityFrameworkCore;

/// <summary>
/// Represents a specification data interface for Entity Framework Core.
/// </summary>
/// <typeparam name="T">The type of entity that the specification is applied to.</typeparam>
public interface IEFCoreSpecificationData<T>
{
    /// <summary>
    /// Gets a value indicating whether the specification should be executed with the AsNoTracking option.
    /// </summary>
    bool IsAsNoTracking { get; }

    /// <summary>
    /// Gets a value indicating whether the specification should be executed with the AsNoTrackingWithIdentityResolution
    /// option.
    /// </summary>
    bool IsAsNoTrackingWithIdentityResolution { get; }

    /// <summary>
    /// Gets a value indicating whether automatic inclusion of related entities should be ignored.
    /// </summary>
    bool IsIgnoreAutoIncludes { get; }

    /// <summary>
    /// Gets a value indicating whether query filters should be ignored when executing the specification.
    /// </summary>
    bool IsIgnoreQueryFilters { get; }

    /// <summary>
    /// Gets a value indicating whether the specification should be executed as a split query.
    /// </summary>
    bool IsAsSplitQuery { get; }

    /// <summary>
    /// Gets a value indicating whether the specification should be executed as a single query.
    /// </summary>
    bool IsAsSingleQuery { get; }

    /// <summary>
    /// Gets the list of related entities to include when executing the specification.
    /// </summary>
    IEnumerable<string> Includes { get; }
}