using PoE2FlipOverlay.Core;
using PoE2FlipOverlay.Core.Models;
using Xunit;

namespace PoE2FlipOverlay.Core.Tests;

public class RatioApproximationTests
{
    [Fact]
    public void Finds_the_simple_pair_from_the_user_example()
    {
        // 14.33 ≈ 43 : 3 (43 / 3 = 14.333) — the case Nicolas described.
        var ratio = RatioApproximation.Closest(14.33m, tolerance: 0.01m);

        Assert.Equal(new IntegerRatio(43, 3), ratio);
    }

    [Fact]
    public void AtLeast_never_goes_below_the_target()
    {
        // Safe for a BUY: the placeable rate must still be >= the intended price.
        foreach (var target in new[] { 14.33m, 14.51m, 14.75m, 3.2m, 21.07m })
        {
            var ratio = RatioApproximation.Closest(target, 0.01m, RoundingDirection.AtLeast);
            Assert.True(ratio.Value >= target, $"{ratio} = {ratio.Value} should be >= {target}");
        }
    }

    [Fact]
    public void AtMost_never_goes_above_the_target()
    {
        // Safe for a SELL: the placeable rate must still be <= the intended price.
        foreach (var target in new[] { 14.74m, 14.5m, 15.09m, 7.8m, 21.07m })
        {
            var ratio = RatioApproximation.Closest(target, 0.01m, RoundingDirection.AtMost);
            Assert.True(ratio.Value <= target, $"{ratio} = {ratio.Value} should be <= {target}");
        }
    }

    [Fact]
    public void Exact_rates_collapse_to_the_smallest_pair()
    {
        Assert.Equal(new IntegerRatio(15, 1), RatioApproximation.Closest(15m, 0.01m));
        Assert.Equal(new IntegerRatio(29, 2), RatioApproximation.Closest(14.5m, 0.01m));
    }

    [Fact]
    public void Stays_within_tolerance_when_possible()
    {
        var ratio = RatioApproximation.Closest(14.33m, 0.01m, RoundingDirection.AtLeast);
        Assert.True(Math.Abs(ratio.Value - 14.33m) <= 0.01m);
    }

    [Fact]
    public void IntegerRatio_exposes_value_and_readable_text()
    {
        var ratio = new IntegerRatio(43, 3);
        Assert.Equal("43 : 3", ratio.ToString());
        Assert.Equal(43m / 3m, ratio.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1.5)]
    public void Rejects_non_positive_targets(decimal target)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RatioApproximation.Closest(target, 0.01m));
    }
}
