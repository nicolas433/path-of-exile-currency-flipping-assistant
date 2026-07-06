using System.Linq;
using PoE2FlipOverlay.Core;
using PoE2FlipOverlay.Core.Models;
using Xunit;

namespace PoE2FlipOverlay.Core.Tests;

/// <summary>
/// Real Currency Exchange books transcribed from in-game screenshots
/// (Fracturing Orb × Divine Orb). Documents what the tool computes from live
/// data and guards against regressions.
/// </summary>
public class RealBookScenarioTests
{
    // "I Want Fracturing / I Have Divine" side shows the asks (1 : X), with stock.
    // Note the top ask (14.50) is a 2-stock dust order.
    private const string AskSidePanel = """
        1 : 14.50    2
        1 : 14.80    100
        1 : 14.80    88
        1 : 14.85    33
        1 : 15       1.054
        < 1 : 15     938
        """;

    // "I Want Divine / I Have Fracturing" side shows the bids (X : 1).
    private const string BidSidePanel = """
        14.67 : 1    440
        14.50 : 1    29
        14.40 : 1    288
        14.33 : 1    43
        14.30 : 1    143
        < 14.30 : 1  38.599
        """;

    private static OrderBook Combined() => new(
        RatioParser.Parse(BidSidePanel).Bids,
        RatioParser.Parse(AskSidePanel).Asks);

    [Fact]
    public void Parses_best_bid_and_ask_and_stock_from_the_real_panels()
    {
        var asks = RatioParser.Parse(AskSidePanel);
        var bids = RatioParser.Parse(BidSidePanel);

        Assert.Equal(new[] { 14.50m, 14.80m, 14.80m, 14.85m, 15m }, asks.Asks.Select(l => l.Ratio).ToArray());
        Assert.Equal(new long[] { 2, 100, 88, 33, 1054 }, asks.Asks.Select(l => l.Stock).ToArray());
        Assert.Equal(14.67m, bids.BestBid()?.Ratio);
    }

    [Fact]
    public void Without_dust_filter_the_book_looks_crossed()
    {
        var book = Combined();

        // Raw top ask is the 2-stock dust at 14.50, below the 14.67 bid -> crossed.
        Assert.Equal(14.67m, book.BestBid()?.Ratio);
        Assert.Equal(14.50m, book.BestAsk()?.Ratio);
    }

    [Fact]
    public void Skipping_dust_reveals_the_real_ask_level()
    {
        var book = Combined();

        // With a stock floor, the 2-stock dust ask is ignored; real ask is 14.80.
        Assert.Equal(14.80m, book.BestAsk(minStock: 10)?.Ratio);
        Assert.Equal(14.67m, book.BestBid(minStock: 10)?.Ratio);
    }
}
