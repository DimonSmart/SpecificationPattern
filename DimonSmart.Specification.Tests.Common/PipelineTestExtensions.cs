using System.Linq.Expressions;

namespace DimonSmart.Specification.Tests.Common;

/// <summary>Assertions inspect the supported pipeline instead of reconstructing legacy state.</summary>
public static class PipelineTestExtensions
{
    public static IReadOnlyList<QueryPipelineStep> OrderingSteps<T>(
        this ISpecification<T> specification) where T : class =>
        specification.PipelineSteps.Where(step => step.IsOrdering).ToList().AsReadOnly();

    public static QueryPipelineStep? LastStep<T>(
        this ISpecification<T> specification, string operation) where T : class =>
        specification.PipelineSteps.LastOrDefault(step => step.Operation == operation);

    public static QueryPipelineStep? WhereStep<T>(
        this ISpecification<T> specification) where T : class =>
        specification.LastStep("Where");
}
