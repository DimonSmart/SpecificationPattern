using TestsCommon;

namespace DimonSmart.EFDBSpecificationTests;

public class DatabaseFixture : TestsBase, IDisposable
{
    public SchoolContext TestDBContext { get; private set; } // = GetFreshDBContext();

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

    public void Dispose()
    {
        TestDBContext.Database.EnsureDeleted();
    }
}