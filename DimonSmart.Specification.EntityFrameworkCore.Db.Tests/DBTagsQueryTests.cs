using System.Reflection;
using System.Runtime.CompilerServices;
using DimonSmart.Specification.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

[Collection("Database collection")]
public class DBTagQueryTests : TestsBase
{
    private readonly SchoolContext _testDBContext;

    public DBTagQueryTests(DatabaseFixture fixture)
    {
        Fixture = fixture;
        _testDBContext = fixture.TestDBContext;
    }

    public DatabaseFixture Fixture { get; }

    [Fact]
    public void QueryShouldBeTaggedWithOneTag()
    {
        // Arrange
        _testDBContext.DbCommandsLog.Clear();
        const string tag = "Name and Age ordered query";

        // Act
        var taggedSpecification = EFCoreSpecification<Student>
            .Create()
            .OrderBy(s => s.Name)
            .OrderBy(s => s.Age)
            .TagWith(tag);

        _ = _testDBContext.BySpecification(taggedSpecification.AsSplitQuery()).ToList();

        // Assert
        _testDBContext.DbCommandsLog
            .Single()
            .Should()
            .Contain(tag);
    }

    [Fact]
    public void QueryShouldBeTaggedWithCallerSideInfo()
    {
        // Arrange
        _testDBContext.DbCommandsLog.Clear();

        // Act
        var taggedSpecification = EFCoreSpecification<Student>
            .Create()
            .OrderBy(s => s.Name)
            .OrderBy(s => s.Age)
            .TagWithCallSite();

        _ = _testDBContext.BySpecification(taggedSpecification.AsSplitQuery()).ToList();

        // Assert
        _testDBContext.DbCommandsLog
            .Single()
            .Should()
            .Contain(GetCallerFilePath());
    }

    [Fact]
    public void TagWithCallSiteShouldIncludeTagOnlyOnce()
    {
        const string tag = "Call-site tag";
        var specification = EFCoreSpecification<Student>.Create()
            .TagWithCallSite(tag, "TestMethod", "TestFile.cs", 42);

        specification.EFCoreSpecificationData.Tag.Should().Be(
            $"{tag}{Environment.NewLine}TestMethod() -> TestFile.cs:42");
    }

    public static string GetCallerFilePath([NotParameterized] [CallerFilePath] string callerFilePath = "")
    {
        return callerFilePath;
    }

    public static string MakePathRelativeToSolution(string filePath)
    {
        var assemblyLocation = Assembly.GetEntryAssembly()!.Location;
        var solutionRoot = Path.GetDirectoryName(assemblyLocation)!;
        var fullPath = Path.GetFullPath(filePath);
        return Path.GetRelativePath(solutionRoot, fullPath);
    }
}