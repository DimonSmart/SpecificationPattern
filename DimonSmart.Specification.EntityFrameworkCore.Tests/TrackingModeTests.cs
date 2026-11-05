using DimonSmart.Specification.Tests.Common;

namespace DimonSmart.Specification.EntityFrameworkCore.Tests;

public class TrackingModeTests
{
    [Fact]
    public void AsTrackingDoesNotMutateSource()
    {
        var original = EFCoreSpecification.Create<Student>().Where(s => s.Age >= 18);
        EFCoreSpecification<Student> tracked = original.AsTracking();

        Assert.False(original.EFCoreSpecificationData.IsAsTracking);
        Assert.False(original.EFCoreSpecificationData.IsAsNoTracking);
        Assert.False(original.EFCoreSpecificationData.IsAsNoTrackingWithIdentityResolution);
        Assert.True(tracked.EFCoreSpecificationData.IsAsTracking);
        Assert.False(tracked.EFCoreSpecificationData.IsAsNoTracking);
        Assert.False(tracked.EFCoreSpecificationData.IsAsNoTrackingWithIdentityResolution);
    }

    [Fact]
    public void LastTrackingSetterWinsWithoutMutatingEarlierSpecifications()
    {
        var first = EFCoreSpecification.Create<Student>().AsTracking();
        var second = first.AsNoTracking();
        var third = second.AsNoTrackingWithIdentityResolution();
        var fourth = third.AsTracking();

        Assert.True(first.EFCoreSpecificationData.IsAsTracking);
        Assert.True(second.EFCoreSpecificationData.IsAsNoTracking);
        Assert.True(third.EFCoreSpecificationData.IsAsNoTrackingWithIdentityResolution);
        Assert.True(fourth.EFCoreSpecificationData.IsAsTracking);
        Assert.False(fourth.EFCoreSpecificationData.IsAsNoTracking);
        Assert.False(fourth.EFCoreSpecificationData.IsAsNoTrackingWithIdentityResolution);
    }

    [Fact]
    public void DefaultModeIsNeutralForAndAndOrInBothDirections()
    {
        var defaults = EFCoreSpecification.Create<Student>();
        var modes = new[]
        {
            defaults.AsTracking(),
            defaults.AsNoTracking(),
            defaults.AsNoTrackingWithIdentityResolution()
        };

        foreach (var mode in modes)
        {
            AssertSameMode(mode, defaults.And(mode));
            AssertSameMode(mode, mode.And(defaults));
            AssertSameMode(mode, defaults.Or(mode));
            AssertSameMode(mode, mode.Or(defaults));
        }

        Assert.False(defaults.And(defaults).EFCoreSpecificationData.IsAsTracking);
        Assert.False(defaults.Or(defaults).EFCoreSpecificationData.IsAsNoTracking);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SameExplicitModesComposeAndDifferentModesConflict(bool useOr)
    {
        var defaults = EFCoreSpecification.Create<Student>();
        var modes = new[]
        {
            defaults.AsTracking(),
            defaults.AsNoTracking(),
            defaults.AsNoTrackingWithIdentityResolution()
        };

        for (var i = 0; i < modes.Length; i++)
        {
            for (var j = 0; j < modes.Length; j++)
            {
                var left = modes[i];
                var right = modes[j];
                if (i == j)
                {
                    AssertSameMode(left, useOr ? left.Or(right) : left.And(right));
                }
                else
                {
                    var exception = Assert.Throws<InvalidOperationException>(
                        () => { _ = useOr ? left.Or(right) : left.And(right); });
                    Assert.Contains("tracking", exception.Message, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }

    private static void AssertSameMode(
        EFCoreSpecification<Student> expected,
        EFCoreSpecification<Student> actual)
    {
        var before = expected.EFCoreSpecificationData;
        var after = actual.EFCoreSpecificationData;
        Assert.Equal(before.IsAsTracking, after.IsAsTracking);
        Assert.Equal(before.IsAsNoTracking, after.IsAsNoTracking);
        Assert.Equal(before.IsAsNoTrackingWithIdentityResolution, after.IsAsNoTrackingWithIdentityResolution);
    }
}
