using FluentAssertions;
using SpecificationPattern;
using static SpecificationPattern.SpecificationExtension;

namespace SpecificationProjectTests
{
    public class WhereUnitTests : TestsBase
    {
     
        [Fact]
        public void SimpleWhereCondition()
        {
            // Arrange
            var specification = Specification<Student>.Where(s => s.Age < 21);
            
            // Act
            var under21 = Students.BySpecification(specification).ToList();

            // Assert
            under21.Should().ContainSingle(i => i == Sofia20);
        }

        [Fact]
        public void WhereWithOrClauseTest()
        {
            // Arrange
            var specification = Specification<Student>
                .Where(s => s.Age < 21)
                .Or(s => s.Age == 30);

            // Act
            var under21 = Students.BySpecification(specification).ToList();

            // Assert
            under21.Should().Contain(Sofia20);
            under21.Should().Contain(Alex30);
            under21.Should().NotContain(Alex22);
        }
    }
}