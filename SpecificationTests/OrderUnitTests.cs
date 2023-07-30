using DimonSmart.Specification;
using FluentAssertions;
using TestsCommon;

namespace SpecificationProjectTests
{
    public class OrderUnitTests : TestsBase
    {

        [Fact]
        public void OneLevelOrderByTest()
        {
            // Arrange
            var specification = Specification<Student>
                .Create()
                .OrderBy(s => s.Age);

            // Act
            var ordered = Students.BySpecification(specification).ToList();

            // Assert
            ordered.Should().BeInAscendingOrder(i => i.Age);
        }


        [Fact]
        public void OneLevelOrderByDescTest()
        {
            // Arrange
            var specification = Specification<Student>
                .Create()
                .OrderByDesc(s => s.Age);

            // Act
            var ordered = Students.BySpecification(specification).ToList();

            // Assert
            ordered.Should().BeInDescendingOrder(i => i.Age);
        }

        [Fact]
        public void TwoLevelOrderByTest()
        {
            // Arrange
            var specification = Specification<Student>
                .Create()
                .As<ISpecification<Student>>()
                .OrderBy(s => s.Name)
                .OrderByDesc(s => s.Age);

            // Act
            var ordered = Students.BySpecification(specification).ToList();

            // Assert
            ordered.Should().BeEquivalentTo(new List<Student> { Alex30, Alex22, Sofia20 });
        }
    }
}