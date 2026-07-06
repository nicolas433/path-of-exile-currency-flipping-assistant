namespace PoE2FlipOverlay.Core.Models;

/// <summary>
/// Which side of the Currency Exchange a captured ratio panel represents.
/// </summary>
public enum BookSide
{
    Unknown,
    SellFracturing, // "X : 1" bids
    BuyFracturing   // "1 : X" asks
}

/// <summary>A single order-book level: a ratio and the stock (volume) available at it.</summary>
public readonly record struct OrderLevel(decimal Ratio, long Stock);

/// <summary>
/// Levels extracted from one or more captures of the ratio panel. Bids are the
/// "X : 1" values (divines per fracturing); asks are the "1 : X" values.
/// </summary>
public sealed class OrderBook
{
    /// <summary>How far a reading may deviate from its side's median before being discarded.</summary>
    public const decimal OutlierTolerance = 0.10m;

    public IReadOnlyList<OrderLevel> Bids { get; }
    public IReadOnlyList<OrderLevel> Asks { get; }

    public OrderBook(IEnumerable<OrderLevel> bids, IEnumerable<OrderLevel> asks)
    {
        Bids = bids.ToList();
        Asks = asks.ToList();
    }

    public bool HasBothSides => Bids.Count > 0 && Asks.Count > 0;

    /// <summary>
    /// Best (highest) bid, ignoring outliers and dust — levels whose stock is
    /// known (&gt; 0) but below <paramref name="minStock"/>. Returns null if empty.
    /// </summary>
    public OrderLevel? BestBid(long minStock = 0) => Best(Bids, minStock, highest: true);

    /// <summary>Best (lowest) ask, ignoring outliers and dust. Returns null if empty.</summary>
    public OrderLevel? BestAsk(long minStock = 0) => Best(Asks, minStock, highest: false);

    private static OrderLevel? Best(IReadOnlyList<OrderLevel> levels, long minStock, bool highest)
    {
        var sane = Sane(levels);
        if (sane.Count == 0) return null;

        // Drop dust (stock known and below the threshold); keep unknown-stock (0).
        var eligible = sane.Where(l => l.Stock <= 0 || l.Stock >= minStock).ToList();
        if (eligible.Count == 0) eligible = sane.ToList();

        return highest ? eligible.MaxBy(l => l.Ratio) : eligible.MinBy(l => l.Ratio);
    }

    public BookSide DetectSide()
    {
        if (Bids.Count > Asks.Count) return BookSide.SellFracturing;
        if (Asks.Count > Bids.Count) return BookSide.BuyFracturing;
        return BookSide.Unknown;
    }

    /// <summary>
    /// Flags a probably-wrong reading: fewer than three lines per side, or a
    /// spread wider than 3%.
    /// </summary>
    public bool IsReadingSuspect(out decimal spreadPercent)
    {
        spreadPercent = 0m;
        if (BestBid()?.Ratio is not { } bid || BestAsk()?.Ratio is not { } ask || bid == 0m)
            return true;

        spreadPercent = (ask - bid) / bid * 100m;
        return spreadPercent > 3m || Bids.Count < 3 || Asks.Count < 3;
    }

    /// <summary>
    /// Discards levels more than <see cref="OutlierTolerance"/> from the median
    /// ratio of their side. Falls back to the original list if that empties it.
    /// </summary>
    public static IReadOnlyList<OrderLevel> Sane(IReadOnlyList<OrderLevel> levels)
    {
        if (levels.Count == 0) return levels;

        var sorted = levels.Select(l => l.Ratio).OrderBy(v => v).ToList();
        var median = sorted[sorted.Count / 2];
        if (median == 0m) return levels;

        var ok = levels.Where(l => Math.Abs(l.Ratio - median) / median <= OutlierTolerance).ToList();
        return ok.Count > 0 ? ok : levels;
    }
}
