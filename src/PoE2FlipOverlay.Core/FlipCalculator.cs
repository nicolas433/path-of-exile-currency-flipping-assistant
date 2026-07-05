using PoE2FlipOverlay.Core.Models;

namespace PoE2FlipOverlay.Core;

/// <summary>
/// Computes a passive-order flip cycle: buy just above the best bid, sell just
/// below the best ask. Ported from the validated <c>calc</c> prototype in
/// <c>flip_calculator.html</c>.
/// </summary>
public static class FlipCalculator
{
    /// <summary>Margins below this percentage are flagged; gold fees likely eat the profit.</summary>
    public const decimal TightMarginThresholdPercent = 0.7m;

    /// <summary>
    /// Calculates buy/sell prices, quantity, profit and margin for one cycle.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// If bid, ask or budget is not positive, or tick is negative.
    /// </exception>
    public static FlipResult Calculate(FlipInput input)
    {
        if (input.BestBid <= 0m)
            throw new ArgumentOutOfRangeException(nameof(input), "Best bid must be positive.");
        if (input.BestAsk <= 0m)
            throw new ArgumentOutOfRangeException(nameof(input), "Best ask must be positive.");
        if (input.Budget <= 0m)
            throw new ArgumentOutOfRangeException(nameof(input), "Budget must be positive.");
        if (input.Tick < 0m)
            throw new ArgumentOutOfRangeException(nameof(input), "Tick cannot be negative.");

        var buy = input.BestBid + input.Tick;   // undercut the buyers' queue
        var sell = input.BestAsk - input.Tick;  // undercut the sellers' queue

        var quantity = (long)Math.Floor(input.Budget / buy);
        var cost = quantity * buy;
        var revenue = quantity * sell;
        var profit = revenue - cost;
        var margin = (sell - buy) / buy * 100m;

        var warning =
            sell - buy <= 0m ? FlipWarning.SpreadClosed :
            margin < TightMarginThresholdPercent ? FlipWarning.TightMargin :
            FlipWarning.None;

        return new FlipResult(buy, sell, quantity, cost, revenue, profit, margin, warning);
    }
}
