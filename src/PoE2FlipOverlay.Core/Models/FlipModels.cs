namespace PoE2FlipOverlay.Core.Models;

/// <summary>Actionable warning attached to a computed flip.</summary>
public enum FlipWarning
{
    /// <summary>Spread is healthy; the flip is worth placing.</summary>
    None,

    /// <summary>Buy price meets or exceeds sell price — placing both orders would lose money.</summary>
    SpreadClosed,

    /// <summary>Margin is below the safe threshold; gold fees will likely eat the profit.</summary>
    TightMargin
}

/// <summary>Inputs for a single flip-cycle calculation.</summary>
/// <param name="BestBid">Highest bid read from the book (divines per fracturing).</param>
/// <param name="BestAsk">Lowest ask read from the book (divines per fracturing).</param>
/// <param name="Budget">Divines available to spend on the buy order.</param>
/// <param name="Tick">Minimum price increment used to undercut the book. Defaults to 0.01.</param>
public readonly record struct FlipInput(
    decimal BestBid,
    decimal BestAsk,
    decimal Budget,
    decimal Tick = 0.01m);

/// <summary>Result of a single flip-cycle calculation.</summary>
/// <param name="BuyPrice">Price to place the buy order at (best bid + tick).</param>
/// <param name="SellPrice">Price to place the sell order at (best ask − tick).</param>
/// <param name="Quantity">Orbs that fit the budget at the buy price.</param>
/// <param name="Cost">Total divines spent buying.</param>
/// <param name="Revenue">Total divines received selling.</param>
/// <param name="Profit">Revenue minus cost for the whole cycle.</param>
/// <param name="MarginPercent">Per-orb margin as a percentage of the buy price.</param>
/// <param name="Warning">Actionable warning about the flip's viability.</param>
public readonly record struct FlipResult(
    decimal BuyPrice,
    decimal SellPrice,
    long Quantity,
    decimal Cost,
    decimal Revenue,
    decimal Profit,
    decimal MarginPercent,
    FlipWarning Warning);

/// <summary>
/// A concrete, placeable flip in whole orbs, scaled to a divine budget.
/// These are the exact numbers to type into the two Currency Exchange orders.
/// </summary>
/// <param name="BuyGiveDivine">Divines you commit on the buy order.</param>
/// <param name="BuyGetFracturing">Fracturing you receive from the buy order.</param>
/// <param name="SellGiveFracturing">Fracturing you commit on the sell order (what you bought).</param>
/// <param name="SellGetDivine">Divines you receive from the sell order.</param>
/// <param name="Profit">Net divines after the full cycle (SellGetDivine − BuyGiveDivine).</param>
public readonly record struct FlipPlan(
    long BuyGiveDivine,
    long BuyGetFracturing,
    long SellGiveFracturing,
    long SellGetDivine,
    decimal Profit)
{
    /// <summary>False when the budget can't afford even one whole unit of the buy ratio.</summary>
    public bool IsPlaceable => BuyGetFracturing > 0;
}
