using System.Globalization;
using PoE2FlipOverlay.Core;
using PoE2FlipOverlay.Core.Models;
using PoE2FlipOverlay.Ocr;

// Tiny harness to check how well Windows OCR reads a captured ratio panel.
// Usage:
//   OcrProbe <image.png> [upscale=2] [budget=431] [tick=0.01]
//
// Point it at one or two screenshots of the ratio panel and it prints the raw
// OCR text, the parsed bids/asks, and (if both sides are present) the flip.

if (args.Length < 1)
{
    Console.WriteLine("Usage: OcrProbe <image1.png> [image2.png] [--upscale N] [--budget N] [--tick N]");
    Console.WriteLine("You can pass one screenshot per side, or a single image with both.");
    return 1;
}

var images = new List<string>();
int upscale = 2;
decimal budget = 431m;
decimal tick = 0.01m;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--upscale": upscale = int.Parse(args[++i]); break;
        case "--budget": budget = ParseDecimal(args[++i]); break;
        case "--tick": tick = ParseDecimal(args[++i]); break;
        default: images.Add(args[i]); break;
    }
}

foreach (var path in images)
{
    if (!File.Exists(path))
    {
        Console.Error.WriteLine($"File not found: {path}");
        return 1;
    }
}

var reader = new ScreenTextReader();
Console.WriteLine($"OCR language: {reader.RecognizerLanguage} | upscale: {upscale}x");
Console.WriteLine();

var allBids = new List<decimal>();
var allAsks = new List<decimal>();

foreach (var path in images)
{
    Console.WriteLine($"=== {Path.GetFileName(path)} ===");
    var text = await reader.ReadFileAsync(path, upscale);

    Console.WriteLine("--- raw OCR text ---");
    Console.WriteLine(text.Length == 0 ? "(nothing recognised)" : text);

    var book = RatioParser.Parse(text);
    Console.WriteLine("--- parsed ---");
    Console.WriteLine($"side guess: {book.DetectSide()}");
    Console.WriteLine($"bids: [{FormatList(book.Bids)}]");
    Console.WriteLine($"asks: [{FormatList(book.Asks)}]");
    Console.WriteLine();

    allBids.AddRange(book.Bids);
    allAsks.AddRange(book.Asks);
}

var merged = new OrderBook(allBids, allAsks);
Console.WriteLine("=== combined book ===");

if (!merged.HasBothSides)
{
    Console.WriteLine("Could not read both sides — need at least one bid and one ask.");
    Console.WriteLine("Check the raw OCR text above to see what went wrong.");
    return 0;
}

var bestBid = merged.BestBid!.Value;
var bestAsk = merged.BestAsk!.Value;
Console.WriteLine($"best bid / best ask: {Fmt(bestBid)} / {Fmt(bestAsk)}");

if (merged.IsReadingSuspect(out var spread))
    Console.WriteLine($"WARNING: reading looks suspect (spread {Fmt(spread)}%, " +
                      $"{merged.Bids.Count} bids / {merged.Asks.Count} asks) — double-check in game.");

var flip = FlipCalculator.Calculate(new FlipInput(bestBid, bestAsk, budget, tick));
Console.WriteLine();
Console.WriteLine("=== flip (budget " + Fmt(budget) + " div, tick " + Fmt(tick) + ") ===");
Console.WriteLine($"BUY  at {Fmt(flip.BuyPrice)}");
Console.WriteLine($"SELL at {Fmt(flip.SellPrice)}");
Console.WriteLine($"quantity: {flip.Quantity}");
Console.WriteLine($"profit:   {Fmt(flip.Profit)} div  (margin {Fmt(flip.MarginPercent)}%)");
if (flip.Warning != FlipWarning.None)
    Console.WriteLine($"WARNING:  {flip.Warning}");

return 0;

static string FormatList(IReadOnlyList<decimal> values) =>
    string.Join(", ", values.Select(Fmt));

static string Fmt(decimal value) =>
    value.ToString("0.##", CultureInfo.InvariantCulture);

static decimal ParseDecimal(string s) =>
    decimal.Parse(s.Replace(',', '.'), CultureInfo.InvariantCulture);
