using DimonSmart.Specification.Tests.Common;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

public class DatabaseFixture : TestsBase, IDisposable
{
    public DatabaseFixture()
    {
        var context = new SchoolContext();
        context.Database.EnsureDeleted();

        context.Authors.AddRange(Authors);
        context.Books.AddRange(Books);
        context.Schools.AddRange(Schools);
        context.Students.AddRange(Students);
        context.SaveChanges();
        TestDBContext = context;
    }

    public SchoolContext TestDBContext { get; } // = GetFreshDBContext();

    public void Dispose()
    {
        TestDBContext.Database.EnsureDeleted();
    }
}