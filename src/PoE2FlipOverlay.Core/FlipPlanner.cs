using PoE2FlipOverlay.Core.Models;

namespace PoE2FlipOverlay.Core;

/// <summary>
/// Scales a flip to a divine budget, producing the largest whole-orb order pair
/// that fits. Works directly from the buy/sell prices, so it behaves for any
/// price scale (rates of 14 or of 267 alike).
/// </summary>
public static class FlipPlanner
{
    /// <summary>
    /// Given how many divines you have and the buy/sell prices (divines per
    /// fracturing), returns the biggest placeable flip: buy as many whole
    /// fracturing as the budget allows (paying a whole number of divines, never
    /// below the buy price), then sell exactly that many.
    /// </summary>
    public static FlipPlan Plan(decimal budgetDivine, decimal buyPrice, decimal sellPrice)
    {
        if (budgetDivine < 0m)
            throw new ArgumentOutOfRangeException(nameof(budgetDivine), "Budget cannot be negative.");
        if (buyPrice <= 0m)
            throw new ArgumentOutOfRangeException(nameof(buyPrice), "Buy price must be positive.");
        if (sellPrice <= 0m)
            throw new ArgumentOutOfRangeException(nameof(sellPrice), "Sell price must be positive.");

        // Largest whole fracturing whose whole-divine cost still fits the budget.
        // Buy cost rounds UP (never bid below the target rate).
        long fracturing = (long)Math.Floor(budgetDivine / buyPrice);
        while (fracturing > 0 && (long)Math.Ceiling(buyPrice * fracturing) > budgetDivine)
            fracturing--;

        long buyGiveDivine = fracturing > 0 ? (long)Math.Ceiling(buyPrice * fracturing) : 0;
        long buyGetFracturing = fracturing;

        // Sell exactly what you bought; divines received round DOWN (never ask above target).
        long sellGiveFracturing = fracturing;
        long sellGetDivine = fracturing > 0 ? (long)Math.Floor(sellPrice * fracturing) : 0;

        decimal profit = sellGetDivine - buyGiveDivine;

        return new FlipPlan(buyGiveDivine, buyGetFracturing, sellGiveFracturing, sellGetDivine, profit);
    }
}
