using PoE2FlipOverlay.Core;
using PoE2FlipOverlay.Core.Models;
using Xunit;

namespace PoE2FlipOverlay.Core.Tests;

public class FlipCalculatorTests
{
    [Fact]
    public void Calculate_undercuts_both_sides_by_one_tick()
    {
        var result = FlipCalculator.Calculate(new FlipInput(BestBid: 14.17m, BestAsk: 14.24m, Budget: 431m));

        Assert.Equal(14.18m, result.BuyPrice);  // bid + tick
        Assert.Equal(14.23m, result.SellPrice); // ask - tick
    }

    [Fact]
    public void Calculate_floors_quantity_to_the_budget()
    {
        var result = FlipCalculator.Calculate(new FlipInput(BestBid: 14.17m, BestAsk: 14.24m, Budget: 431m));

        // floor(431 / 14.18) = floor(30.39...) = 30
        Assert.Equal(30, result.Quantity);
        Assert.Equal(30 * 14.18m, result.Cost);
        Assert.Equal(30 * 14.23m, result.Revenue);
        Assert.Equal(1.50m, result.Profit);
    }

    [Fact]
    public void Calculate_flags_tight_margin()
    {
        // ~0.35% margin — below the 0.7% threshold.
        var result = FlipCalculator.Calculate(new FlipInput(BestBid: 14.17m, BestAsk: 14.24m, Budget: 431m));

        Assert.Equal(FlipWarning.TightMargin, result.Warning);
        Assert.Equal(0.35m, Math.Round(result.MarginPercent, 2));
    }

    [Fact]
    public void Calculate_flags_closed_spread()
    {
        // After ticking, buy (14.31) > sell (14.29): the flip loses money.
        var result = FlipCalculator.Calculate(new FlipInput(BestBid: 14.30m, BestAsk: 14.30m, Budget: 100m));

        Assert.Equal(FlipWarning.SpreadClosed, result.Warning);
        Assert.True(result.Profit < 0m);
    }

    [Fact]
    public void Calculate_reports_no_warning_for_a_healthy_spread()
    {
        var result = FlipCalculator.Calculate(new FlipInput(BestBid: 14.00m, BestAsk: 15.00m, Budget: 1000m));

        Assert.Equal(FlipWarning.None, result.Warning);
        Assert.True(result.Profit > 0m);
        Assert.Equal(7.00m, Math.Round(result.MarginPercent, 2));
    }

    [Fact]
    public void Calculate_respects_a_custom_tick()
    {
        var result = FlipCalculator.Calculate(new FlipInput(BestBid: 14.00m, BestAsk: 15.00m, Budget: 1000m, Tick: 0.05m));

        Assert.Equal(14.05m, result.BuyPrice);
        Assert.Equal(14.95m, result.SellPrice);
    }

    [Theory]
    [InlineData(0, 14.24, 431)]
    [InlineData(14.17, 0, 431)]
    [InlineData(14.17, 14.24, 0)]
    public void Calculate_rejects_non_positive_inputs(decimal bid, decimal ask, decimal budget)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FlipCalculator.Calculate(new FlipInput(bid, ask, budget)));
    }
}
