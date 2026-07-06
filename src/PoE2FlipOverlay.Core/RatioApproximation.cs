using PoE2FlipOverlay.Core.Models;

namespace PoE2FlipOverlay.Core;

/// <summary>
/// Finds the simplest integer ratio (fewest orbs) that represents a decimal
/// rate closely enough to place as an order. "Simplest" = smallest denominator
/// whose value is within tolerance of the target.
/// </summary>
public static class RatioApproximation
{
    public const int DefaultMaxDenominator = 200;

    /// <summary>
    /// Returns the smallest-denominator <see cref="IntegerRatio"/> whose value is
    /// within <paramref name="tolerance"/> of <paramref name="target"/>, honouring
    /// the rounding <paramref name="direction"/>. If nothing reaches the tolerance
    /// up to <paramref name="maxDenominator"/>, returns the closest pair found
    /// (still on the requested side).
    /// </summary>
    public static IntegerRatio Closest(
        decimal target,
        decimal tolerance,
        RoundingDirection direction = RoundingDirection.Nearest,
        int maxDenominator = DefaultMaxDenominator)
    {
        if (target <= 0m) throw new ArgumentOutOfRangeException(nameof(target), "Target must be positive.");
        if (tolerance < 0m) throw new ArgumentOutOfRangeException(nameof(tolerance), "Tolerance cannot be negative.");
        if (maxDenominator < 1) maxDenominator = 1;

        IntegerRatio best = default;
        var bestError = decimal.MaxValue;

        for (long q = 1; q <= maxDenominator; q++)
        {
            var scaled = target * q;
            long p = direction switch
            {
                RoundingDirection.AtLeast => (long)Math.Ceiling(scaled),
                RoundingDirection.AtMost => (long)Math.Floor(scaled),
                _ => (long)Math.Round(scaled, MidpointRounding.AwayFromZero)
            };
            if (p <= 0) continue;

            var value = (decimal)p / q;
            var error = Math.Abs(value - target);

            if (error < bestError)
            {
                bestError = error;
                best = new IntegerRatio(p, q);
            }

            if (error <= tolerance)
                return new IntegerRatio(p, q); // smallest denominator that's good enough
        }

        return best; // best effort when nothing hits the tolerance
    }
}
