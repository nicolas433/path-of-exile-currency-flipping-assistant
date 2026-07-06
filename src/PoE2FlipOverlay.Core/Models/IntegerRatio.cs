namespace PoE2FlipOverlay.Core.Models;

/// <summary>
/// Which way an integer-ratio approximation may round relative to the target
/// rate. For a passive order this keeps you on the safe side of the book.
/// </summary>
public enum RoundingDirection
{
    /// <summary>Closest in absolute value, above or below.</summary>
    Nearest,

    /// <summary>Result is ≥ target — safe for a BUY (you never underbid the intended price).</summary>
    AtLeast,

    /// <summary>Result is ≤ target — safe for a SELL (you never overask the intended price).</summary>
    AtMost
}

/// <summary>
/// A rate expressed as whole orbs: give <see cref="Numerator"/> of one currency
/// to get <see cref="Denominator"/> of the other. This is what you can actually
/// type into the Currency Exchange, since it only takes integer amounts.
/// </summary>
public readonly record struct IntegerRatio(long Numerator, long Denominator)
{
    /// <summary>The decimal rate this pair represents (numerator ÷ denominator).</summary>
    public decimal Value => (decimal)Numerator / Denominator;

    public override string ToString() => $"{Numerator} : {Denominator}";
}
