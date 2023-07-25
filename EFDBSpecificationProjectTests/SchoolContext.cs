using Microsoft.EntityFrameworkCore;
using TestsCommon;

namespace EFDBSpecificationProjectTests
{
    public class SchoolContext : DbContext
    {
        public DbSet<Author> Authors { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<School> Schools { get; set; }
        public DbSet<Student> Students { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

            optionsBuilder.UseInMemoryDatabase("SchoolDB");
        }
    }
}