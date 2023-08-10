using DimonSmart.Specification.Tests.Common;
using FluentAssertions;

namespace DimonSmart.Specification.EntityFrameworkCore.Tests;

public class IncludeTests
{
    [Fact]
    public void IncludeTest()
    {
        // Arrange
        var specification = EFCoreSpecification<Student>
            .Create()
            .Where(s => s.Age < 21);

        // Act
        specification.Include(s => s.School.MainBook.Author);

        // Assert
        specification
            .EFCoreSpecificationData.Includes
            .Should()
            .BeEquivalentTo(new List<string> { "School.MainBook.Author" });
    }

    [Fact]
    public void IncludeChainTest()
    {
        // Arrange
        var specification = EFCoreSpecification<Student>
            .Create()
            .Where(s => s.Age < 21);

        // Act
        specification
            .Include(s => s.School)
            .Include(s => s.School.MainBook)
            .Include(s => s.School.MainBook.Author);

        // Assert
        specification
            .EFCoreSpecificationData.Includes
            .Should()
            .BeEquivalentTo(new List<string> { "School.MainBook.Author" });
    }

    [Fact]
    public void IncludeChainReverseTest()
    {
        // Arrange
        var specification = EFCoreSpecification<Student>
            .Create()
            .Where(s => s.Age < 21);

        // Act
        specification
            .Include(s => s.School.MainBook.Author)
            .Include(s => s.School.MainBook)
            .Include(s => s.School);

        // Assert
        specification
            .EFCoreSpecificationData.Includes
            .Should()
            .BeEquivalentTo(new List<string> { "School.MainBook.Author" });
    }

    [Fact]
    public void IncludeWithThenIncludeTest()
    {
        // Arrange
        var specification = EFCoreSpecification<Student>
            .Create()
            .Where(s => s.Age < 21);

        // Act
        specification
            .Include(s => s.Books)
            .ThenInclude(b => b.Author)
            .ThenInclude(a => a.Name);

        // Assert
        specification
            .EFCoreSpecificationData.Includes
            .Should()
            .BeEquivalentTo(new List<string> { "Books.Author.Name" });
    }
}