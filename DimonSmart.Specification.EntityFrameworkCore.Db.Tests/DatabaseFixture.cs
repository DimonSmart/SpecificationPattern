using DimonSmart.Specification.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

public class DatabaseFixture : TestsBase, IDisposable
{
    public DatabaseFixture()
    {
        var context = new SchoolContext(DbLog);
        context.Database.OpenConnection();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        context.Authors.AddRange(Authors);
        context.Books.AddRange(Books);
        context.Schools.AddRange(Schools);
        context.SaveChanges();
        context.Students.AddRange(Students);
        context.SaveChanges();
        TestDBContext = context;
    }

    public SchoolContext TestDBContext { get; }
    public List<string> DbLog { get; } = new();

    public void Dispose()
    {
        TestDBContext.Database.EnsureDeleted();
        GC.SuppressFinalize(this);
    }
}