using DimonSmart.EFSpecification;
using FluentAssertions;
using TestsCommon;

namespace DimonSmart.EFDBSpecificationTests;

public class DBOrderTests : DBTestBase
{
    [Fact]
    public void OneLevelOrderByTest()
    {
        // Arrange
        using var context = GetFreshDBContext();
        var specification = EFSpecification<Student>
            .Create()
            .OrderBy(s => s.Age);

        // Act
        var ordered = context.BySpecification(specification).ToList();

        // Assert
        ordered.Should().BeInAscendingOrder(i => i.Age);
    }

    [Fact]
    public void OneLevelOrderByDescTest()
    {
        // Arrange
        using var context = GetFreshDBContext();
        var specification = EFSpecification<Student>
            .Create()
            .OrderByDesc(s => s.Age);

        // Act
        var ordered = context.BySpecification(specification).ToList();

        // Assert
        ordered.Should().BeInDescendingOrder(i => i.Age);
    }

    [Fact]
    public void TwoLevelOrderByTest()
    {
        // Arrange
        using var context = GetFreshDBContext();
        var specification = EFSpecification<Student>
            .Create()
            .OrderBy(s => s.Name)
            .OrderByDesc(s => s.Age);

        // Act
        var ordered = context.BySpecification(specification).ToList();

        // Assert
        ordered.Should().BeEquivalentTo(new List<Student> { Alex30, Alex22, Sofia20 });
    }
}