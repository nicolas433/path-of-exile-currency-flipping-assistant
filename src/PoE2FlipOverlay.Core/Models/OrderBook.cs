namespace PoE2FlipOverlay.Core.Models;

/// <summary>
/// Which side of the Currency Exchange a captured ratio panel represents.
/// In PoE2 the ratio panel always shows the order book of the OPPOSITE side
/// of the order you are creating.
/// </summary>
public enum BookSide
{
    Unknown,

    /// <summary>
    /// Panel dominated by "X : 1" lines — buyers offering X divines for 1
    /// fracturing (bids). This is what you see while creating a SELL order.
    /// </summary>
    SellFracturing,

    /// <summary>
    /// Panel dominated by "1 : X" lines — sellers asking X divines per
    /// fracturing (asks). This is what you see while creating a BUY order.
    /// </summary>
    BuyFracturing
}

/// <summary>
/// Ratios extracted from one or more captures of the ratio panel.
/// <para>Bids are the "X : 1" values (divines offered per 1 fracturing);
/// asks are the "1 : X" values (divines asked per 1 fracturing).</para>
/// Readings from several captures can be merged into a single book.
/// </summary>
public sealed class OrderBook
{
    /// <summary>How far a reading may deviate from its side's median before being discarded.</summary>
    public const decimal OutlierTolerance = 0.10m;

    public IReadOnlyList<decimal> Bids { get; }
    public IReadOnlyList<decimal> Asks { get; }

    public OrderBook(IEnumerable<decimal> bids, IEnumerable<decimal> asks)
    {
        Bids = bids.ToList();
        Asks = asks.ToList();
    }

    public bool HasBothSides => Bids.Count > 0 && Asks.Count > 0;

    /// <summary>Best (highest) bid after discarding outliers; <c>null</c> if no bids were read.</summary>
    public decimal? BestBid => Sane(Bids) is { Count: > 0 } s ? s.Max() : null;

    /// <summary>Best (lowest) ask after discarding outliers; <c>null</c> if no asks were read.</summary>
    public decimal? BestAsk => Sane(Asks) is { Count: > 0 } s ? s.Min() : null;

    /// <summary>
    /// Guesses which side this book was captured from by comparing how many
    /// bid vs ask lines were read.
    /// </summary>
    public BookSide DetectSide()
    {
        if (Bids.Count > Asks.Count) return BookSide.SellFracturing;
        if (Asks.Count > Bids.Count) return BookSide.BuyFracturing;
        return BookSide.Unknown;
    }

    /// <summary>
    /// Flags a reading that is probably wrong: fewer than three lines on either
    /// side, or a spread wider than 3% (a line was likely skipped at the top).
    /// </summary>
    /// <param name="spreadPercent">The computed spread, or 0 when a side is missing.</param>
    public bool IsReadingSuspect(out decimal spreadPercent)
    {
        spreadPercent = 0m;
        if (BestBid is not { } bid || BestAsk is not { } ask || bid == 0m)
            return true;

        spreadPercent = (ask - bid) / bid * 100m;
        return spreadPercent > 3m || Bids.Count < 3 || Asks.Count < 3;
    }

    /// <summary>
    /// Discards readings more than <see cref="OutlierTolerance"/> away from the
    /// median of their own side (defends against OCR misreads). Falls back to
    /// the original list if the filter would empty it.
    /// </summary>
    public static IReadOnlyList<decimal> Sane(IReadOnlyList<decimal> values)
    {
        if (values.Count == 0) return values;

        var sorted = values.OrderBy(v => v).ToList();
        var median = sorted[sorted.Count / 2];
        if (median == 0m) return values;

        var ok = values.Where(v => Math.Abs(v - median) / median <= OutlierTolerance).ToList();
        return ok.Count > 0 ? ok : values;
    }
}
