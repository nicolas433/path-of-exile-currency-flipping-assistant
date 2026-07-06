using System.Globalization;
using System.Text.RegularExpressions;
using PoE2FlipOverlay.Core.Models;

namespace PoE2FlipOverlay.Core;

/// <summary>
/// Turns the raw text of a ratio panel (typically OCR output) into an
/// <see cref="OrderBook"/>. Ported from the validated <c>parseRatios</c>
/// prototype in <c>flip_calculator.html</c>.
/// </summary>
public static partial class RatioParser
{
    // A "left : right" ratio anywhere in the line. Groups capture each side.
    [GeneratedRegex(@"([\d.,]+)\s*:\s*([\d.,]+)")]
    private static partial Regex RatioLine();

    // Leading numeric run of a token — mirrors JS parseFloat, which stops at a
    // second decimal point (e.g. "1.234.56" -> 1.234).
    [GeneratedRegex(@"\d*\.?\d+")]
    private static partial Regex LeadingNumber();

    // A stock number after the ratio (e.g. "40", "7.434"); separators are thousands.
    [GeneratedRegex(@"\d[\d.,]*")]
    private static partial Regex StockToken();

    // The game marks aggregated worse offers with a leading "<". OCR often
    // renders that glyph as a guillemet or other angle character, so we treat
    // any of these as the aggregate marker.
    private static readonly char[] AggregateMarkers =
    {
        '<',        // less-than
        '«',   // « left-pointing double angle quotation mark
        '‹',   // ‹ single left-pointing angle quotation mark
        '≤',   // ≤ less-than-or-equal
        '⟨',   // ⟨ mathematical left angle bracket
    };

    /// <summary>
    /// Parses every "X : Y" ratio line in <paramref name="text"/>, classifying
    /// each as a bid ("X : 1") or ask ("1 : X"). Lines with a "&lt;" aggregate
    /// marker are ignored, and ";" is treated as ":" (a common OCR misread).
    /// </summary>
    public static OrderBook Parse(string? text)
    {
        var bids = new List<OrderLevel>();
        var asks = new List<OrderLevel>();

        if (string.IsNullOrEmpty(text))
            return new OrderBook(bids, asks);

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Replace(';', ':').Trim();
            if (!line.Contains(':') || line.IndexOfAny(AggregateMarkers) >= 0)
                continue;

            var match = RatioLine().Match(line);
            if (!match.Success)
                continue;

            var left = ToNum(match.Groups[1].Value);
            var right = ToNum(match.Groups[2].Value);
            if (left is null || right is null)
                continue;

            // The stock column sits after the ratio on the same line.
            long stock = ExtractStock(line, match.Index + match.Length);

            // "14.20 : 1" -> someone paying divines per fracturing (BID)
            // "1 : 14.38" -> someone asking divines per fracturing (ASK)
            if (IsOne(right.Value) && left.Value is > 1m and < 1000m)
                bids.Add(new OrderLevel(left.Value, stock));
            else if (IsOne(left.Value) && right.Value is > 1m and < 1000m)
                asks.Add(new OrderLevel(right.Value, stock));
        }

        return new OrderBook(bids, asks);
    }

    /// <summary>Reads the stock number that follows the ratio on the line (0 if none).</summary>
    private static long ExtractStock(string line, int from)
    {
        if (from < 0 || from >= line.Length)
            return 0;

        var match = StockToken().Match(line[from..]);
        if (!match.Success)
            return 0;

        var digits = match.Value.Replace(".", "").Replace(",", "");
        return long.TryParse(digits, out var stock) ? stock : 0;
    }

    private static bool IsOne(decimal value) => Math.Abs(value - 1m) < 0.001m;

    /// <summary>
    /// Parses a numeric token, accepting "," or "." as the decimal separator.
    /// Returns <c>null</c> if no number can be read.
    /// </summary>
    internal static decimal? ToNum(string token)
    {
        var normalized = token.Replace(',', '.');
        var match = LeadingNumber().Match(normalized);
        if (!match.Success)
            return null;

        return decimal.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var n)
            ? n
            : null;
    }
}
