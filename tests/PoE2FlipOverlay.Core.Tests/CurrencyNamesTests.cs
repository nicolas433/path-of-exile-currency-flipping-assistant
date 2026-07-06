using PoE2FlipOverlay.Core;
using Xunit;

namespace PoE2FlipOverlay.Core.Tests;

public class CurrencyNamesTests
{
    [Fact]
    public void Detects_the_pair_in_appearance_order()
    {
        // Roughly what OCR of the exchange header yields (buy side).
        var (first, second) = CurrencyNames.DetectPair("I Want Fracturing Orb 2 1 : 14.50 29 Divine Orb I Have");

        Assert.Equal("Fracturing Orb", first);
        Assert.Equal("Divine Orb", second);
    }

    [Fact]
    public void Order_follows_the_text_even_when_reversed()
    {
        var (first, second) = CurrencyNames.DetectPair("I Want Divine Orb ... Fracturing Orb I Have");

        Assert.Equal("Divine Orb", first);
        Assert.Equal("Fracturing Orb", second);
    }

    [Fact]
    public void Matches_multiword_names_and_is_case_insensitive()
    {
        var (first, second) = CurrencyNames.DetectPair("i want mirror of kalandra 1 : 6350 divine orb i have");

        Assert.Equal("Mirror of Kalandra", first);
        Assert.Equal("Divine Orb", second);
    }

    [Fact]
    public void Returns_nulls_when_nothing_matches()
    {
        var (first, second) = CurrencyNames.DetectPair("no currencies here 1 : 2");

        Assert.Null(first);
        Assert.Null(second);
    }
}
