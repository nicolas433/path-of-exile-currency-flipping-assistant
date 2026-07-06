using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using PoE2FlipOverlay.Core;
using PoE2FlipOverlay.Core.Models;
using PoE2FlipOverlay.Ocr;
using WinForms = System.Windows.Forms;
// Enabling WinForms also imports System.Drawing, so pin these to their WPF types.
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace PoE2FlipOverlay.App;

public partial class OverlayWindow : Window
{
    private OverlayConfig _config = new();
    private readonly Dictionary<int, Action> _hotkeyActions = new();
    private readonly List<string> _failedHotkeys = new();
    private int _nextHotkeyId = 1;

    private IntPtr _hwnd;
    private bool _interactive; // false = click-through (game receives the clicks)

    private WinForms.NotifyIcon? _tray;
    private bool _suppressBudgetEvent;

    // Capture / OCR state (Step C).
    private ScreenTextReader? _reader;
    private IReadOnlyList<decimal> _bids = Array.Empty<decimal>();
    private IReadOnlyList<decimal> _asks = Array.Empty<decimal>();
    private string? _wantName;
    private string? _haveName;

    private readonly HistoryStore _history = new();

    // Remembered so we can re-render when the budget changes.
    private OrderBook _lastBook = new(Array.Empty<decimal>(), Array.Empty<decimal>());
    private FlipResult _lastResult;

    public OverlayWindow()
    {
        InitializeComponent();
        SourceInitialized += OnSourceInitialized;
        Closed += OnClosed;
        MouseLeftButtonDown += OnMouseLeftButtonDown;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _config = OverlayConfig.LoadOrCreate();
        _hwnd = new WindowInteropHelper(this).Handle;

        _suppressBudgetEvent = true;
        BudgetInput.Text = _config.Budget.ToString("0.##", CultureInfo.InvariantCulture);
        _suppressBudgetEvent = false;

        // Start interactive so the panel is usable right away (move it, set your
        // divines). Toggle to click-through with Ctrl+Shift+F (or the tray) to play.
        SetInteractive(true);
        HwndSource.FromHwnd(_hwnd)?.AddHook(WndProc);
        RegisterConfiguredHotkeys();
        SetupTray();

        _history.Load();
        HistoryList.ItemsSource = _history.Entries;
        UpdateHistoryEmpty();

        // Step A: show a demo flip so we can confirm the overlay renders over the game.
        ShowDemo();
    }

    // ---- Tray icon: always-available way to show/hide and quit ----

    private void SetupTray()
    {
        _tray = new WinForms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "PoE2 Flip Overlay"
        };

        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add("Mostrar / esconder", null, (_, _) => ToggleVisible());
        menu.Items.Add("Modo interativo (mover)", null, (_, _) => SetInteractive(!_interactive));
        menu.Items.Add("Calibrar região do painel…", null, (_, _) => CalibrateRegion());
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("Sair", null, (_, _) => Close());
        _tray.ContextMenuStrip = menu;
        _tray.DoubleClick += (_, _) => ToggleVisible();
    }

    private void CalibrateRegion()
    {
        // Hide the overlay so it isn't part of the region being drawn over.
        var wasVisible = Visibility == Visibility.Visible;
        Visibility = Visibility.Hidden;
        try
        {
            var cal = new CalibrationWindow(
                "Desenhe UM retângulo cobrindo os NOMES das moedas E a lista de ratios") { Owner = this };

            if (cal.ShowDialog() == true && cal.Result is not null)
            {
                _config.Capture = cal.Result;
                _config.TrySave();
                HintText.Text = $"região salva: {cal.Result} — Num4/Num5 capturam";
            }
        }
        finally
        {
            if (wasVisible) Visibility = Visibility.Visible;
        }
    }

    private void ToggleVisible()
    {
        if (Visibility == Visibility.Visible)
        {
            Visibility = Visibility.Hidden;
        }
        else
        {
            Visibility = Visibility.Visible;
            Topmost = true;
        }
    }

    // ---- Hotkeys ----

    private void RegisterConfiguredHotkeys()
    {
        var h = _config.Hotkeys;
        Register(h.HideOverlay, () => Visibility = Visibility.Hidden);
        Register(h.ShowOverlay, ShowOverlay);
        Register(h.CaptureBuy, () => OnCaptureRequested("compra"));
        Register(h.CaptureSell, () => OnCaptureRequested("venda"));
        Register(h.ToggleInteractive, () => SetInteractive(!_interactive));
        Register(h.Quit, Close);

        if (_failedHotkeys.Count > 0)
            HintText.Text = "⚠ teclas não registradas: " + string.Join(", ", _failedHotkeys) +
                            " (conflito? edite config.json)";
    }

    private void Register(string spec, Action action)
    {
        if (!HotkeyDefinition.TryParse(spec, out var hk))
        {
            _failedHotkeys.Add(spec);
            return;
        }

        int id = _nextHotkeyId++;
        if (NativeMethods.RegisterHotKey(_hwnd, id, hk.Modifiers, hk.VirtualKey))
            _hotkeyActions[id] = action;
        else
            _failedHotkeys.Add(spec);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        foreach (var id in _hotkeyActions.Keys)
            NativeMethods.UnregisterHotKey(_hwnd, id);

        _tray?.Dispose();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && _hotkeyActions.TryGetValue(wParam.ToInt32(), out var action))
        {
            action();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void ShowOverlay()
    {
        Visibility = Visibility.Visible;
        Topmost = true;
    }

    private async void OnCaptureRequested(string side)
    {
        if (_config.Capture is null)
        {
            HintText.Text = "calibre primeiro: clique em ⚙ calibrar região do painel";
            return;
        }

        try
        {
            HintText.Text = $"capturando ({side})…";

            var png = ScreenCapture.CaptureToPng(_config.Capture, "exchange");

            _reader ??= new ScreenTextReader();
            var readout = await _reader.ReadDetailedFileAsync(png, upscaleFactor: 1);
            var text = readout.Text;

            // Scrape the two names by position (works for any currency); fall back
            // to the known-currency list if a side comes out empty.
            var (want, have) = ScrapeNames(readout.Words);
            var (knownFirst, knownSecond) = CurrencyNames.DetectPair(text);
            want ??= knownFirst;
            have ??= knownSecond;
            if (!string.IsNullOrWhiteSpace(want)) _wantName = want;
            if (!string.IsNullOrWhiteSpace(have)) _haveName = have;

            var book = RatioParser.Parse(text);
            if (book.Bids.Count > 0) _bids = book.Bids; // sell-side panel
            if (book.Asks.Count > 0) _asks = book.Asks; // buy-side panel

            UpdateHeader();

            var combined = new OrderBook(_bids, _asks);
            if (combined.HasBothSides)
            {
                var flip = FlipCalculator.Calculate(
                    new FlipInput(combined.BestBid!.Value, combined.BestAsk!.Value, _config.Budget, _config.Tick));
                Render(combined, flip);

                _history.Record(PairLabel(), combined.BestBid!.Value, combined.BestAsk!.Value,
                    flip.MarginPercent, flip.Profit);
                UpdateHistoryEmpty();

                var suspect = combined.IsReadingSuspect(out _) ? " ⚠ leitura suspeita, confira" : "";
                HintText.Text = $"lido: {combined.Bids.Count} bids / {combined.Asks.Count} asks{suspect}";
            }
            else
            {
                var missing = _bids.Count == 0 ? "venda (bids)" : "compra (asks)";
                HintText.Text = $"lido ({side}): {book.Bids.Count} bids / {book.Asks.Count} asks — falta capturar o lado {missing}";
            }
        }
        catch (Exception ex)
        {
            HintText.Text = "erro na captura: " + ex.Message;
        }
    }

    private void UpdateHistoryEmpty()
    {
        HistoryEmpty.Visibility = _history.Entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Stable label for the current pair (order-independent) for history keying.</summary>
    private string PairLabel()
    {
        var a = string.IsNullOrWhiteSpace(_wantName) ? null : _wantName!.Trim();
        var b = string.IsNullOrWhiteSpace(_haveName) ? null : _haveName!.Trim();

        if (a is null && b is null) return "par não identificado";
        if (a is null || b is null) return $"{a ?? b} × ?";

        return string.CompareOrdinal(a, b) <= 0 ? $"{a} × {b}" : $"{b} × {a}";
    }

    private void UpdateHeader()
    {
        if (!string.IsNullOrWhiteSpace(_wantName) && !string.IsNullOrWhiteSpace(_haveName))
            HeaderTitle.Text = $"⚖ {_wantName} × {_haveName}";
    }

    // Words that are UI labels, not currency names.
    private static readonly HashSet<string> NameStopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "I", "Want", "Have", "Market", "Ratio", "Stock", "Currency", "Exchange",
        "Order", "Listed", "Buying", "Selling", "Place"
    };

    /// <summary>
    /// Reads the two currency names by position: the name-row band, split into a
    /// left cluster ("I Want") and a right cluster ("I Have"), ignoring labels.
    /// Bands are tuned to the default 1920×1080 capture region.
    /// </summary>
    private static (string?, string?) ScrapeNames(IReadOnlyList<OcrWord> words)
    {
        bool InNameBand(OcrWord w) =>
            w.CenterY is >= 0.38 and <= 0.62 &&
            w.Text.Any(char.IsLetter) &&
            !NameStopwords.Contains(w.Text.Trim());

        string? Join(IEnumerable<OcrWord> ws)
        {
            var s = string.Join(" ", ws.OrderBy(w => w.CenterX).Select(w => w.Text.Trim())).Trim();
            return s.Length >= 2 ? s : null;
        }

        var band = words.Where(InNameBand).ToList();
        var left = Join(band.Where(w => w.CenterX <= 0.42));
        var right = Join(band.Where(w => w.CenterX >= 0.58));
        return (left, right);
    }

    private void SetInteractive(bool interactive)
    {
        _interactive = interactive;
        NativeMethods.SetClickThrough(_hwnd, enabled: !interactive);

        ModeIndicator.Fill = interactive
            ? (Brush)FindResource("ProfitPos")
            : (Brush)FindResource("TextFaint");

        HintText.Text = interactive
            ? "🟢 interativo (clicável) · arraste p/ mover · Ctrl+Shift+F → click-through p/ jogar"
            : "⚪ click-through (cliques passam pro jogo) · Ctrl+Shift+F → interativo p/ editar";
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Only draggable in interactive mode (in click-through mode we never get the event).
        // Don't start a drag when the click lands on the budget box.
        if (_interactive && e.ButtonState == MouseButtonState.Pressed && !(e.OriginalSource is System.Windows.Controls.TextBox))
            DragMove();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void OnCalibrateClick(object sender, RoutedEventArgs e) => CalibrateRegion();

    private void OnBudgetChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_suppressBudgetEvent) return;

        var text = BudgetInput.Text.Replace(',', '.');
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var budget) && budget > 0m)
        {
            _config.Budget = budget;
            _config.TrySave();
            Render(_lastBook, _lastResult); // re-scale the orders
        }
    }

    private void ShowDemo()
    {
        // Chosen so the placeable orders land on tidy pairs: buy 14.33 = 43:3, sell 15.00 = 15:1.
        var book = new OrderBook(bids: new[] { 14.32m }, asks: new[] { 15.01m });
        var result = FlipCalculator.Calculate(
            new FlipInput(book.BestBid!.Value, book.BestAsk!.Value, _config.Budget, _config.Tick));
        Render(book, result);
    }

    /// <summary>Fills the panel from a computed flip. Reused by later steps.</summary>
    public void Render(OrderBook book, FlipResult result)
    {
        _lastBook = book;
        _lastResult = result;

        BuyPrice.Text = Fmt(result.BuyPrice);
        SellPrice.Text = Fmt(result.SellPrice);
        BookRead.Text = $"{Fmt(book.BestBid ?? 0)} / {Fmt(book.BestAsk ?? 0)}";
        MarginValue.Text = Fmt(result.MarginPercent) + "%";

        // Largest whole-orb order the budget affords, on the safe side of the book.
        var plan = FlipPlanner.Plan(_config.Budget, result.BuyPrice, result.SellPrice);

        if (plan.IsPlaceable)
        {
            BuyOrder.Text = $"{plan.BuyGiveDivine} div → {plan.BuyGetFracturing} frac";
            SellOrder.Text = $"{plan.SellGiveFracturing} frac → {plan.SellGetDivine} div";
            Profit.Text = (plan.Profit >= 0 ? "+" : "") + Fmt(plan.Profit) + " div";
            Profit.Foreground = (Brush)FindResource(plan.Profit > 0 ? "ProfitPos" : "ProfitNeg");
        }
        else
        {
            BuyOrder.Text = "orçamento insuficiente";
            SellOrder.Text = "—";
            Profit.Text = "—";
        }

        ShowWarning(result.Warning);
    }

    private void ShowWarning(FlipWarning warning)
    {
        (string? text, bool bad) = warning switch
        {
            FlipWarning.SpreadClosed => ("✖ Spread fechado — não flipe agora, você perderia dinheiro.", true),
            FlipWarning.TightMargin => ("⚠ Margem abaixo de 0,7% — o gold provavelmente come o lucro.", false),
            _ => (null, false)
        };

        if (text is null)
        {
            WarningBox.Visibility = Visibility.Collapsed;
            return;
        }

        WarningBox.Visibility = Visibility.Visible;
        WarningText.Text = text;
        WarningBox.Background = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(bad ? "#2E1A16" : "#2E2816"));
        WarningBox.BorderBrush = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(bad ? "#6E3428" : "#6E5C28"));
        WarningText.Foreground = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(bad ? "#E8A396" : "#E8D496"));
    }

    private static string Fmt(decimal value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);
}
