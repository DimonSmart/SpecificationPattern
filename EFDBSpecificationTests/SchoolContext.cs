using Microsoft.EntityFrameworkCore;
using TestsCommon;

namespace DimonSmart.EFDBSpecificationTests;

public class SchoolContext : DbContext
{
    public DbSet<Author> Authors { get; set; } = null!;
    public DbSet<Book> Books { get; set; } = null!;
    public DbSet<School> Schools { get; set; } = null!;
    public DbSet<Student> Students { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase("SchoolDB");
    }
}