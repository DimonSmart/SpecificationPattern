using DimonSmart.Specification.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

public class SchoolContext : DbContext
{
    public readonly IList<string> DbCommandsLog;

    public SchoolContext(IList<string> logTo)
    {
        DbCommandsLog = logTo;
    }

    public DbSet<Author> Authors { get; set; } = null!;
    public DbSet<Book> Books { get; set; } = null!;
    public DbSet<School> Schools { get; set; } = null!;
    public DbSet<Student> Students { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseSqlite("Data Source=:memory:")
            .AddInterceptors(new EFCoreSqlLoggingInterceptor(DbCommandsLog));
    }
}