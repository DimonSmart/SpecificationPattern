namespace DimonSmart.Specification.EntityFrameworkCore;

/// <inheritdoc />
/// <summary>
/// Interface for building specifications for Entity Framework Core queries.
/// </summary>
/// <typeparam name="T">The type of entity being queried.</typeparam>
public interface IEFCoreSpecification<T> : IBaseSpecification<T, IEFCoreSpecification<T>> where T : class
{
    /// <summary>
    /// Gets the specification data that contains Entity Framework Core specific data.
    /// </summary>
    IEFCoreSpecificationData<T> EFCoreSpecificationData { get; }

    /// <summary>
    /// Adds an "Include" statement to the query, specifying related entities to be loaded.
    /// </summary>
    /// <param name="include">The path of the related entity to include in the query.</param>
    /// <param name="resetIncludeLevel">Whether to reset the include level (default is true).</param>
    void AddInclude(string include, bool resetIncludeLevel = true);

    /// <summary>
    /// Specifies that the query should be executed with "NoTracking" behavior.
    /// </summary>
    /// <returns>The current specification with "NoTracking" applied.</returns>
    IEFCoreSpecification<T> AsNoTracking();

    /// <summary>
    /// Specifies that the query should be executed with "SplitQuery" behavior.
    /// </summary>
    /// <returns>The current specification with "SplitQuery" applied.</returns>
    IEFCoreSpecification<T> AsSplitQuery();

    /// <summary>
    /// Specifies that the query should be executed with "SingleQuery" behavior.
    /// </summary>
    /// <returns>The current specification with "SingleQuery" applied.</returns>
    IEFCoreSpecification<T> AsSingleQuery();

    /// <summary>
    /// Specifies that the query should be executed with "NoTracking" and identity resolution behavior.
    /// </summary>
    /// <returns>The current specification with "NoTracking" and identity resolution applied.</returns>
    IEFCoreSpecification<T> AsNoTrackingWithIdentityResolution();

    /// <summary>
    /// Specifies to ignore automatically included related entities in the query.
    /// </summary>
    /// <returns>The current specification with automatic includes ignored.</returns>
    IEFCoreSpecification<T> IgnoreAutoIncludes();

    /// <summary>
    /// Specifies to ignore query filters defined in the model.
    /// </summary>
    /// <returns>The current specification with query filters ignored.</returns>
    IEFCoreSpecification<T> IgnoreQueryFilters();

    /// <summary>
    /// Combines the current specification with another specification using the logical OR operator.
    /// </summary>
    /// <remarks>
    /// This method combines the filtering (Where) expressions and included entities
    /// from the specified specification using the logical OR operator.
    /// </remarks>
    /// <param name="or">The specification to combine with using OR.</param>
    /// <returns>The combined specification using OR.</returns>
    IEFCoreSpecification<T> Or(IEFCoreSpecification<T> or);

    /// <summary>
    /// Combines the current specification with another specification using the logical AND operator.
    /// </summary>
    /// <remarks>
    /// This method combines the filtering (Where) expressions and included entities
    /// from the specified specification using the logical AND operator.
    /// </remarks>
    /// <param name="and">The specification to combine with using AND.</param>
    /// <returns>The combined specification using AND.</returns>
    IEFCoreSpecification<T> And(IEFCoreSpecification<T> and);

    /// <summary>
    /// Annotate generated query with tag specified
    /// </summary>
    /// <param name="tag">Query tag, name</param>
    /// <returns>The current specification with query tagged.</returns>
    IEFCoreSpecification<T> TagWith(string tag);
}