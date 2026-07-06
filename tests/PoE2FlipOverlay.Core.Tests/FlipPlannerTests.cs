using PoE2FlipOverlay.Core;
using Xunit;

namespace PoE2FlipOverlay.Core.Tests;

public class FlipPlannerTests
{
    [Fact]
    public void Scales_the_order_up_to_the_budget()
    {
        // 431 divines, buy 14.33, sell 15.00 -> 30 fracturing fit.
        var plan = FlipPlanner.Plan(431m, buyPrice: 14.33m, sellPrice: 15.00m);

        Assert.Equal(30, plan.BuyGetFracturing);   // floor(431 / 14.33)
        Assert.Equal(430, plan.BuyGiveDivine);      // ceil(14.33 * 30)
        Assert.Equal(30, plan.SellGiveFracturing);
        Assert.Equal(450, plan.SellGetDivine);      // floor(15 * 30)
        Assert.Equal(20m, plan.Profit);
        Assert.True(plan.IsPlaceable);
    }

    [Fact]
    public void Never_spends_more_than_the_budget()
    {
        var plan = FlipPlanner.Plan(100m, buyPrice: 14.33m, sellPrice: 15.00m);

        Assert.True(plan.BuyGiveDivine <= 100);
        Assert.Equal(6, plan.BuyGetFracturing);  // floor(100 / 14.33) = 6
        Assert.Equal(86, plan.BuyGiveDivine);    // ceil(14.33 * 6)
    }

    [Fact]
    public void Handles_high_priced_pairs_that_afford_only_one() // regression for "orçamento insuficiente"
    {
        // ~267 divines per fracturing; 473 affords exactly one.
        var plan = FlipPlanner.Plan(473m, buyPrice: 267.01m, sellPrice: 269.99m);

        Assert.True(plan.IsPlaceable);
        Assert.Equal(1, plan.BuyGetFracturing);
        Assert.Equal(268, plan.BuyGiveDivine);   // ceil(267.01)
        Assert.Equal(269, plan.SellGetDivine);   // floor(269.99)
    }

    [Fact]
    public void Not_placeable_only_when_budget_is_below_one_unit()
    {
        var plan = FlipPlanner.Plan(10m, buyPrice: 14.33m, sellPrice: 15.00m);

        Assert.False(plan.IsPlaceable);
        Assert.Equal(0, plan.BuyGetFracturing);
    }

    [Fact]
    public void Rejects_negative_budget()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FlipPlanner.Plan(-1m, 14.33m, 15m));
    }
}
