using DimonSmart.EFSpecification;
using TestsCommon;

namespace DimonSmart.EFSpecificationTests
{
    public class UsageSamplesTest
    {
        [Fact]
        public void SequentialWhere()
        {
            var specification = EFSpecification<Student>.Create()
                .Where(s => s.Age < 21)
                .Where(s => s.Age > 16);
        }

        [Fact]
        public void SequentialInclude()
        {
            IEFIncludeSpecification<Student, School> s1 = EFSpecification<Student>.Create()
                .Include(s => s.School);


            IEFIncludeSpecification<Student, Book> s2 = EFSpecification<Student>.Create()
                .Include(s => s.School.MainBook);
            IEFIncludeSpecification<Student, List<Book>> s3 = EFSpecification<Student>.Create()
                .Include(s => s.Books);


            var specification = EFSpecification<Student>.Create()
                .Include(s => s.School)
                .Include(s => s.School.MainBook)
                .Include(s => s.Books)
                .ThenInclude(b => b.Author)
                .Where(s => (s.Age & 1) == 1)
                .Include(s => s.Books);
        }

        [Fact]
        public void MixedWhereInclude()
        {
            var specification = EFSpecification<Student>.Create()
                .Include(s => s.School)
                .Where(s => s.Age < 21)
                .Include(s => s.School.MainBook)
                .Where(s => s.Age > 16);
        }
    }
}