# PoE2 Flip Overlay

A screen-reading overlay for **Path of Exile 2** that watches the Currency
Exchange order book (e.g. Fracturing Orb × Divine Orb, but generic for any
pair) and shows, in real time, the best buy price, the best sell price and the
expected profit of a flip cycle.

> **The tool only reads the screen and displays information.** It never reads
> game memory and never sends input to the game — *you* create the orders. This
> keeps it in the same category as tools like Awakened PoE Trade and within
> GGG's Terms of Service.

## How the flip works

The Currency Exchange ratio panel always shows the order book of the **opposite**
side of the order you are creating:

- `X : 1` lines are **bids** — buyers offering `X` divines for 1 fracturing. The
  first line is the best bid.
- `1 : X` lines are **asks** — sellers asking `X` divines per fracturing. The
  first line is the lowest ask.
- Lines starting with `<` are aggregates of worse offers and are ignored.

The strategy is passive orders (never at market):

| Value | Formula |
| --- | --- |
| Buy price | best bid **+** tick (default `0.01`) — jump the buyers' queue |
| Sell price | lowest ask **−** tick — undercut the sellers |
| Quantity | `floor(budget ÷ buy price)` |
| Cycle profit | `quantity × (sell − buy)` |
| Margin % | `(sell − buy) ÷ buy` |

Warnings: spread ≤ 0 → *don't flip*; margin < 0.7% → *gold will likely eat the
profit*; spread > 3% or fewer than 3 lines per side → *probable OCR error,
double-check*.

## Project layout

```
poe2-flip-overlay/
├── src/
│   └── PoE2FlipOverlay.Core/        # Pure business logic — parsing + calculation
│                                    # Cross-platform (net8.0), no UI/Windows deps
├── tests/
│   └── PoE2FlipOverlay.Core.Tests/  # xUnit tests for the Core logic
└── PoE2FlipOverlay.sln
```

The screen-capture, OCR, overlay and calibration layers (Windows-only,
`.NET 8 + WPF`) build on top of `Core` and are added in later milestones.

## Building and testing

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
dotnet test        # build everything and run the Core tests
dotnet build       # build only
```

The `Core` library and its tests are platform-neutral and run on Windows, Linux
and macOS. The eventual WPF app targets `net8.0-windows` and only builds on
Windows.

## Status

Working end-to-end: the overlay reads the live game and shows the flip.

- [x] Tested business logic (ratio parsing, outlier filtering, flip calc, warnings) — 44 tests
- [x] Screen capture of the exchange region + image preprocessing (upscale + grayscale + contrast)
- [x] OCR via `Windows.Media.Ocr`
- [x] One-rectangle calibration (with a 1080p default) + configurable numpad hotkeys
- [x] Click-through WPF overlay, tray icon, editable budget, whole-orb order sizing
- [x] Currency-name scraping by OCR word position (works for any pair)
- [x] Per-pair history panel
- [ ] Read the Stock column → skip dust orders → thin-top warning
- [ ] English/Portuguese language toggle
- [ ] Phase 2: multi-pair profiles, watch mode, cycle P&L

## Credits

Business logic ported from a validated HTML/JS prototype (`flip_calculator.html`).

## License

[MIT](LICENSE)
