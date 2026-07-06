using PoE2FlipOverlay.Core;
using PoE2FlipOverlay.Core.Models;
using Xunit;

namespace PoE2FlipOverlay.Core.Tests;

/// <summary>
/// Real Currency Exchange books transcribed from in-game screenshots
/// (Fracturing Orb × Divine Orb). Documents exactly what the tool computes
/// from live data and guards against regressions.
/// </summary>
public class RealBookScenarioTests
{
    // "I Want Fracturing / I Have Divine" side shows the asks (1 : X).
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

    [Fact]
    public void Parses_best_bid_and_ask_from_the_real_panels()
    {
        var asks = RatioParser.Parse(AskSidePanel);
        var bids = RatioParser.Parse(BidSidePanel);

        Assert.Equal(new[] { 14.50m, 14.80m, 14.80m, 14.85m, 15m }, asks.Asks);
        Assert.Equal(new[] { 14.67m, 14.50m, 14.40m, 14.33m, 14.30m }, bids.Bids);

        var book = new OrderBook(bids.Bids, asks.Asks);
        Assert.Equal(14.67m, book.BestBid);
        Assert.Equal(14.50m, book.BestAsk);
    }

    [Fact]
    public void Aggressive_flip_on_this_crossed_book_is_spread_closed()
    {
        var book = new OrderBook(
            RatioParser.Parse(BidSidePanel).Bids,
            RatioParser.Parse(AskSidePanel).Asks);

        var flip = FlipCalculator.Calculate(new FlipInput(book.BestBid!.Value, book.BestAsk!.Value, Budget: 431m));

        // best bid 14.67 > best ask 14.50 -> the +tick/-tick maker flip loses money.
        Assert.Equal(14.68m, flip.BuyPrice);
        Assert.Equal(14.49m, flip.SellPrice);
        Assert.Equal(FlipWarning.SpreadClosed, flip.Warning);
        Assert.True(flip.Profit < 0m);
    }
}
