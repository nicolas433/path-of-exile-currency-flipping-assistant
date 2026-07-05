using PoE2FlipOverlay.Core;
using PoE2FlipOverlay.Core.Models;
using Xunit;

namespace PoE2FlipOverlay.Core.Tests;

public class RatioParserTests
{
    // Roughly what OCR yields for the "I Have Fracturing" (sell) side: X : 1 lines.
    private const string SellSidePanel = """
        14.17 : 1    284
        14.15 : 1    1203
        14.10 : 1    900
        < 13.86 : 1  aggregate
        """;

    // The "I Want Fracturing" (buy) side: 1 : X lines.
    private const string BuySidePanel = """
        1 : 14.24   500
        1 : 14.30   200
        1 : 14.38   1000
        """;

    [Fact]
    public void Parse_reads_bids_from_x_to_one_lines()
    {
        var book = RatioParser.Parse(SellSidePanel);

        Assert.Equal(new[] { 14.17m, 14.15m, 14.10m }, book.Bids);
        Assert.Empty(book.Asks);
    }

    [Fact]
    public void Parse_reads_asks_from_one_to_x_lines()
    {
        var book = RatioParser.Parse(BuySidePanel);

        Assert.Equal(new[] { 14.24m, 14.30m, 14.38m }, book.Asks);
        Assert.Empty(book.Bids);
    }

    [Fact]
    public void Parse_ignores_aggregate_lines_starting_with_less_than()
    {
        var book = RatioParser.Parse(SellSidePanel);

        Assert.DoesNotContain(13.86m, book.Bids);
    }

    [Fact]
    public void Parse_treats_semicolon_as_colon()
    {
        // OCR frequently reads ':' as ';'.
        var book = RatioParser.Parse("14.20 ; 1\n1 ; 14.40");

        Assert.Contains(14.20m, book.Bids);
        Assert.Contains(14.40m, book.Asks);
    }

    [Fact]
    public void Parse_accepts_comma_as_decimal_separator()
    {
        var book = RatioParser.Parse("14,17 : 1\n1 : 14,24");

        Assert.Contains(14.17m, book.Bids);
        Assert.Contains(14.24m, book.Asks);
    }

    [Fact]
    public void Parse_discards_values_outside_plausible_range()
    {
        // left > 1000 is rejected; the bare "1 : 1" is neither bid nor ask.
        var book = RatioParser.Parse("5000 : 1\n1 : 1");

        Assert.Empty(book.Bids);
        Assert.Empty(book.Asks);
    }

    [Fact]
    public void BestBid_and_BestAsk_pick_the_top_of_each_side()
    {
        var book = new OrderBook(
            bids: new[] { 14.17m, 14.15m, 14.10m },
            asks: new[] { 14.24m, 14.30m, 14.38m });

        Assert.Equal(14.17m, book.BestBid);
        Assert.Equal(14.24m, book.BestAsk);
    }

    [Fact]
    public void Sane_drops_readings_far_from_the_median()
    {
        // 99.99 is an OCR glitch far from the 14.x cluster.
        var book = new OrderBook(bids: new[] { 14.17m, 14.15m, 99.99m }, asks: Array.Empty<decimal>());

        Assert.Equal(14.17m, book.BestBid); // glitch excluded, real best survives
    }

    [Fact]
    public void DetectSide_uses_which_side_has_more_lines()
    {
        Assert.Equal(BookSide.SellFracturing, RatioParser.Parse(SellSidePanel).DetectSide());
        Assert.Equal(BookSide.BuyFracturing, RatioParser.Parse(BuySidePanel).DetectSide());
    }

    [Fact]
    public void IsReadingSuspect_flags_too_few_lines()
    {
        var thin = new OrderBook(bids: new[] { 14.17m }, asks: new[] { 14.24m });

        Assert.True(thin.IsReadingSuspect(out _));
    }

    [Fact]
    public void IsReadingSuspect_is_false_for_a_clean_tight_book()
    {
        var book = new OrderBook(
            bids: new[] { 14.17m, 14.15m, 14.10m },
            asks: new[] { 14.24m, 14.30m, 14.38m });

        Assert.False(book.IsReadingSuspect(out var spread));
        Assert.True(spread is > 0m and < 3m);
    }

    [Fact]
    public void Parse_of_empty_or_null_returns_empty_book()
    {
        Assert.False(RatioParser.Parse(null).HasBothSides);
        Assert.False(RatioParser.Parse("").HasBothSides);
        Assert.False(RatioParser.Parse("no ratios here").HasBothSides);
    }
}
