using DimonSmart.Specification.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

[Collection("Database collection")]
public sealed class DynamicSpecificationDbTests
{
    private readonly SchoolContext _db;

    public DynamicSpecificationDbTests(DatabaseFixture fixture) => _db = fixture.TestDBContext;

    [Fact]
    public void DynamicSpecificationKeepsSqlAndExistingQueryableSemantics()
    {
        var query = DocumentationExamples.DynamicEFCoreSpecificationExample.Query(
            _db, 20, true, true, 2, true);
        var sql = query.ToQueryString();
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LIMIT", sql, StringComparison.OrdinalIgnoreCase);

        var actual = query.Select(s => s.Id).ToArray();
        var expected = _db.Students.Where(s => s.Age >= 20)
            .OrderBy(s => s.Name).Take(2).Select(s => s.Id).ToArray();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CompositionConflictsAreRejectedBeforeAnyDatabaseCommand()
    {
        var left = EFCoreSpecification.Create<Student>().OrderBy(s => s.Age);
        var right = EFCoreSpecification.Create<Student>().OrderBy(s => s.Name);
        _db.DbCommandsLog.Clear();
        Assert.Throws<InvalidOperationException>(() => left.And(right));
        Assert.Throws<InvalidOperationException>(() => left.Or(right));
        Assert.Throws<InvalidOperationException>(() => left.EnsureCompatibleWith(right));
        Assert.Empty(_db.DbCommandsLog);
    }
}
