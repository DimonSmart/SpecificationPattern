using DimonSmart.EFSpecification;
using DimonSmart.Specification;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TestsCommon;

namespace DimonSmart.EFDBSpecificationTests
{
    public class DBTests : DBTestBase
    {
        [Fact]
        public void SimpleWhereConditionTest()
        {
            // Arrange
            using var context = GetFreshDBContext();
            var specification = EFSpecification<Student>
                .Create()
                .Where(s => s.Age < 21);

            // Act
            var under21 = context.Students.BySpecification(specification).ToList();

            // Assert
            under21.Should().BeEquivalentTo(new List<Student> { Sofia20 });
        }

        [Fact]
        public void WhereWithOrClauseTest()
        {
            // Arrange
            using var context = GetFreshDBContext();
            var specification1 = EFSpecification<Student>
                .Create()
                .Where(s => s.Age < 21);

            var specification2 = EFSpecification<Student>
                .Create()
                .Where(s => s.Age == 30);

            var specification = specification1.Or(specification2);

            // Act
            var under21 = context.Students.BySpecification(specification).ToList();

            // Assert
            under21.Should().BeEquivalentTo(new List<Student> { Sofia20, Alex30 });
        }

        [Fact]
        public void IncludeTest()
        {
            // Arrange
            using var context = GetFreshDBContext();
            var specification = EFSpecification<Student>
                .Create()
                .Where(s => s.Age < 21)
                .Include(s => s.School.MainBook.Author);

            // Act
            var under21 = context.BySpecification(specification).AsNoTracking().ToList();

            // Assert
            under21
                .Should()
                .BeEquivalentTo(new List<Student> { Sofia20 }, options => options.Excluding(e => e.Books));
        }

        [Fact]
        public void OrderOneLevelOrderByTest()
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
        public void OrderOneLevelOrderByDescTest()
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
}