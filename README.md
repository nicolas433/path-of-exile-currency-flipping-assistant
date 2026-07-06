# PoE Currency Flipping Assistant

*🌐 [English](README.md) · [Português](README.pt-BR.md)*

A screen-reading overlay for **Path of Exile 2** (and, thanks to the near-identical
layout, likely **PoE 1**) that watches the Currency Exchange order book and shows,
in real time, the best buy/sell prices and the profit of a flip cycle — with the
exact whole-orb orders to place.

![PoE Currency Flipping Assistant running over the Currency Exchange](docs/screenshot.png)

> **It only reads the screen and displays information.** It never reads game
> memory and never automates input — *you* create the orders. This keeps it in
> the same category as tools like Awakened PoE Trade and within GGG's Terms of
> Service.

## Features

- **Live capture + OCR** of the exchange panel (`Windows.Media.Ocr`), with image
  preprocessing (upscale + grayscale + contrast) so the small game font reads well.
- **Any currency pair** — currency names are scraped from the screen by position,
  no hardcoded list.
- **Whole-orb orders** sized to your budget: it tells you exactly how much to give
  and receive on each order (e.g. `430 Divine → 30 Fracturing`).
- **Stock awareness** — ignores dust orders (tiny volume) and warns about a thin
  top of book, so a lone outlier doesn't fool the verdict.
- **Warnings**: closed spread, margin too tight for gold fees, suspect OCR reading.
- **Per-pair history** of your last readings.
- Transparent, click-through overlay; global hotkeys; tray icon; one-rectangle
  calibration.

## How the flip works

The ratio panel shows the order book of the **opposite** side of the order you're
creating:

| Value | Formula |
| --- | --- |
| Buy price | best bid **+** tick — jump the buyers' queue |
| Sell price | lowest ask **−** tick — undercut the sellers |
| Quantity | `floor(budget ÷ buy price)` |
| Cycle profit | `quantity × (sell − buy)` |

Warnings fire when the spread is closed, the margin is below ~0.7% (gold fees eat
it), or the reading looks unreliable.

## Running it

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (Windows).

- Double-click **`run.bat`** — it rebuilds and launches the overlay.
- Or from a terminal: `dotnet build src\PoE2FlipOverlay.App` then run the produced
  `PoE2FlipOverlay.exe`.

**In game:** open the Currency Exchange with the ratio list visible, then press the
capture hotkey on the buy side (`Num4`) and on the sell side (`Num5`). Toggle
click-through / interactive with `Ctrl+Shift+F`. Settings live in `config.json`
next to the executable.

## Building & testing

```bash
dotnet test        # builds and runs the Core tests (cross-platform)
```

The `Core` library (parsing, flip math, order sizing, currency detection) is
platform-neutral and covered by xUnit tests. The WPF app and OCR target
`net8.0-windows` and only build on Windows.

## Project layout

```
src/PoE2FlipOverlay.Core/   # pure business logic (net8.0) + tests
src/PoE2FlipOverlay.Ocr/    # Windows.Media.Ocr wrapper
src/PoE2FlipOverlay.App/    # WPF overlay
tools/OcrProbe/             # console tool to test OCR on a screenshot
```

## Status

Working end-to-end. Next up: English/Portuguese UI toggle, a one-click
distributable build, and phase-2 ideas (multi-pair profiles, watch mode).

## License

[MIT](LICENSE)
