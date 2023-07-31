﻿using TestsCommon;

namespace DimonSmart.EFDBSpecificationTests;

public class DBTestBase : TestsBase
{
    protected static SchoolContext GetFreshDBContext()
    {
        var context = new SchoolContext();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        context.Authors.AddRange(Authors);
        context.Books.AddRange(Books);
        context.Schools.AddRange(Schools);
        context.Students.AddRange(Students);
        context.SaveChanges();
        return context;
    }
}