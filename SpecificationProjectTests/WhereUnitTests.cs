using FluentAssertions;
using SpecificationPattern;
using TestsCommon;
using static SpecificationPattern.SpecificationExtension;

namespace SpecificationProjectTests
{
    public class WhereUnitTests : TestsBase
    {

        [Fact]
        public void SimpleWhereConditionTest()
        {
            // Arrange
            var specification = Specification<Student>.Create().Where(s => s.Age < 21);

            // Act
            var under21 = Students.BySpecification(specification).ToList();

            // Assert
            under21.Should().BeEquivalentTo(new List<Student> { Sofia20 });
        }

        [Fact]
        public void WhereWithOrClauseTest()
        {
            // Arrange
            var specification1 = Specification<Student>
                .Create()
                .Where(s => s.Age < 21);

            var specification2 = Specification<Student>
                .Create()
                .Where(s => s.Age == 30);

            var specification = specification1.Or(specification2);

            // Act
            var under21 = Students.BySpecification(specification).ToList();

            // Assert
            under21.Should().BeEquivalentTo(new List<Student> { Sofia20, Alex30 });
        }
    }
}