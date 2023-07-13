using FluentAssertions;
using SpecificationPattern;
using static SpecificationPattern.SpecificationExtension;

namespace SpecificationProjectTests
{
    public class OrderUnitTests : TestsBase
    {

        [Fact]
        public void SimpleWhereCondition()
        {
            // Arrange
            var specification = Specification<Student>.Create().OrderBy(s => s.Age);

            // Act
            var ordered = Students.BySpecification(specification).ToList();

            // Assert
            ordered.Should().BeInAscendingOrder(i => i.Age);
        }
    }
}