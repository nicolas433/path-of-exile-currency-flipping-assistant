using System.Linq;
using PoE2FlipOverlay.Core;
using PoE2FlipOverlay.Core.Models;
using Xunit;

namespace PoE2FlipOverlay.Core.Tests;

public class RatioParserTests
{
    private const string SellSidePanel = """
        14.17 : 1    284
        14.15 : 1    1203
        14.10 : 1    900
        < 13.86 : 1  aggregate
        """;

    private const string BuySidePanel = """
        1 : 14.24   500
        1 : 14.30   200
        1 : 14.38   1000
        """;

    private static decimal[] Ratios(IEnumerable<OrderLevel> levels) => levels.Select(l => l.Ratio).ToArray();

    [Fact]
    public void Parse_reads_bids_from_x_to_one_lines()
    {
        var book = RatioParser.Parse(SellSidePanel);

        Assert.Equal(new[] { 14.17m, 14.15m, 14.10m }, Ratios(book.Bids));
        Assert.Empty(book.Asks);
    }

    [Fact]
    public void Parse_reads_asks_from_one_to_x_lines()
    {
        var book = RatioParser.Parse(BuySidePanel);

        Assert.Equal(new[] { 14.24m, 14.30m, 14.38m }, Ratios(book.Asks));
        Assert.Empty(book.Bids);
    }

    [Fact]
    public void Parse_reads_the_stock_column()
    {
        var book = RatioParser.Parse(SellSidePanel);

        Assert.Equal(new long[] { 284, 1203, 900 }, book.Bids.Select(l => l.Stock).ToArray());
    }

    [Fact]
    public void Parse_reads_stock_with_thousands_separators()
    {
        var book = RatioParser.Parse("14.20 : 1   7.434\n14.10 : 1   74.517");

        Assert.Equal(new long[] { 7434, 74517 }, book.Bids.Select(l => l.Stock).ToArray());
    }

    [Fact]
    public void Parse_ignores_aggregate_lines_starting_with_less_than()
    {
        var book = RatioParser.Parse(SellSidePanel);

        Assert.DoesNotContain(13.86m, Ratios(book.Bids));
    }

    [Theory]
    [InlineData("< 13.86 : 1")]
    [InlineData("« 13.86 : 1")]
    [InlineData("‹ 13.86 : 1")]
    [InlineData("≤ 13.86 : 1")]
    public void Parse_ignores_aggregate_markers_however_ocr_renders_them(string aggregateLine)
    {
        var book = RatioParser.Parse("14.50 : 1\n" + aggregateLine);

        Assert.Equal(new[] { 14.50m }, Ratios(book.Bids));
    }

    [Fact]
    public void Parse_treats_semicolon_as_colon()
    {
        var book = RatioParser.Parse("14.20 ; 1\n1 ; 14.40");

        Assert.Contains(14.20m, Ratios(book.Bids));
        Assert.Contains(14.40m, Ratios(book.Asks));
    }

    [Fact]
    public void Parse_accepts_comma_as_decimal_separator()
    {
        var book = RatioParser.Parse("14,17 : 1\n1 : 14,24");

        Assert.Contains(14.17m, Ratios(book.Bids));
        Assert.Contains(14.24m, Ratios(book.Asks));
    }

    [Fact]
    public void BestBid_and_BestAsk_pick_the_top_of_each_side()
    {
        var book = new OrderBook(
            bids: new[] { new OrderLevel(14.17m, 100), new OrderLevel(14.15m, 100), new OrderLevel(14.10m, 100) },
            asks: new[] { new OrderLevel(14.24m, 100), new OrderLevel(14.30m, 100), new OrderLevel(14.38m, 100) });

        Assert.Equal(14.17m, book.BestBid()?.Ratio);
        Assert.Equal(14.24m, book.BestAsk()?.Ratio);
    }

    [Fact]
    public void BestBid_skips_dust_below_the_min_stock()
    {
        // Top bid 14.50 is a 2-stock dust order; the real level is 14.40 with volume.
        var book = new OrderBook(
            bids: new[] { new OrderLevel(14.50m, 2), new OrderLevel(14.40m, 300), new OrderLevel(14.30m, 500) },
            asks: Array.Empty<OrderLevel>());

        Assert.Equal(14.50m, book.BestBid()?.Ratio);        // no filter -> raw top
        Assert.Equal(14.40m, book.BestBid(minStock: 10)?.Ratio); // dust skipped
    }

    [Fact]
    public void Sane_drops_readings_far_from_the_median()
    {
        var book = new OrderBook(
            bids: new[] { new OrderLevel(14.17m, 100), new OrderLevel(14.15m, 100), new OrderLevel(99.99m, 100) },
            asks: Array.Empty<OrderLevel>());

        Assert.Equal(14.17m, book.BestBid()?.Ratio);
    }

    [Fact]
    public void DetectSide_uses_which_side_has_more_lines()
    {
        Assert.Equal(BookSide.SellFracturing, RatioParser.Parse(SellSidePanel).DetectSide());
        Assert.Equal(BookSide.BuyFracturing, RatioParser.Parse(BuySidePanel).DetectSide());
    }

    [Fact]
    public void Parse_of_empty_or_null_returns_empty_book()
    {
        Assert.False(RatioParser.Parse(null).HasBothSides);
        Assert.False(RatioParser.Parse("").HasBothSides);
        Assert.False(RatioParser.Parse("no ratios here").HasBothSides);
    }
}
