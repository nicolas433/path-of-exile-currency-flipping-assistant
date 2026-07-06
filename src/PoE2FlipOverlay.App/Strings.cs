namespace PoE2FlipOverlay.App;

/// <summary>
/// UI strings for one language. Defaults are Portuguese; <see cref="English"/>
/// overrides them. Parameterised strings use {0}, {1}… with string.Format.
/// </summary>
public sealed class Strings
{
    // Panel labels
    public string HeaderDefault = "⚖ Flip — Fracturing × Divine";
    public string BuyAt = "COMPRAR a";
    public string SellAt = "VENDER a";
    public string BidAskRead = "bid / ask lidos";
    public string BudgetHave = "{0} que tenho";           // {0} = value currency
    public string OrdersHeader = "ordens a colocar (orbs inteiros)";
    public string BuyOrder = "COMPRA";
    public string SellOrder = "VENDA";
    public string CycleProfit = "lucro do ciclo";
    public string Margin = "margem";
    public string BudgetInsufficient = "orçamento insuficiente";
    public string CalibrateButton = "⚙ calibrar região do painel";
    public string HistoryTitle = "histórico";
    public string HistoryEmpty = "capture um par pra registrar";

    // Hints (bottom line)
    public string HintInteractive = "🟢 interativo (clicável) · arraste p/ mover · Ctrl+Shift+F → click-through p/ jogar";
    public string HintClickThrough = "⚪ click-through (cliques passam pro jogo) · Ctrl+Shift+F → interativo p/ editar";

    // Capture / status
    public string SideBuy = "compra";
    public string SideSell = "venda";
    public string Capturing = "capturando ({0})…";
    public string CalibrateFirst = "calibre primeiro: clique em ⚙ calibrar região do painel";
    public string CaptureError = "erro na captura: {0}";
    public string ReadStatus = "lido: {0} bids / {1} asks";
    public string MissingSide = "lido ({0}): {1} bids / {2} asks — falta capturar o lado {3}";
    public string MissingBids = "venda (bids)";
    public string MissingAsks = "compra (asks)";
    public string NoteDust = "pulei ordem-poeira";
    public string NoteThin = "topo fininho";
    public string NoteSuspect = "leitura suspeita, confira";
    public string RegionSaved = "região salva: {0} — Num4/Num5 capturam";
    public string HotkeysFailed = "⚠ teclas não registradas: {0} (conflito? edite config.json)";

    // Warnings
    public string WarnSpreadClosed = "✖ Spread fechado — não flipe agora, você perderia dinheiro.";
    public string WarnTightMargin = "⚠ Margem abaixo de 0,7% — o gold provavelmente come o lucro.";

    // Tray
    public string TrayShowHide = "Mostrar / esconder";
    public string TrayInteractive = "Modo interativo (mover)";
    public string TrayCalibrate = "Calibrar região do painel…";
    public string TrayLanguage = "English";          // label to switch TO the other language
    public string TrayQuit = "Sair";

    // Calibration window
    public string CalibInstruction = "Desenhe UM retângulo cobrindo os NOMES das moedas E a lista de ratios · Esc cancela";

    // App
    public string AlreadyOpen =
        "O PoE2 Flip Overlay já está aberto.\n\nFeche a instância atual (bandeja → Sair, ou Ctrl+Shift+X) antes de abrir de novo.";

    public static Strings Get(string? language) =>
        string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? English() : new Strings();

    private static Strings English() => new()
    {
        HeaderDefault = "⚖ Flip — Fracturing × Divine",
        BuyAt = "BUY at",
        SellAt = "SELL at",
        BidAskRead = "bid / ask read",
        BudgetHave = "{0} I have",
        OrdersHeader = "orders to place (whole orbs)",
        BuyOrder = "BUY",
        SellOrder = "SELL",
        CycleProfit = "cycle profit",
        Margin = "margin",
        BudgetInsufficient = "budget too low",
        CalibrateButton = "⚙ calibrate panel region",
        HistoryTitle = "history",
        HistoryEmpty = "capture a pair to record it",

        HintInteractive = "🟢 interactive (clickable) · drag to move · Ctrl+Shift+F → click-through to play",
        HintClickThrough = "⚪ click-through (clicks pass to the game) · Ctrl+Shift+F → interactive to edit",

        SideBuy = "buy",
        SideSell = "sell",
        Capturing = "capturing ({0})…",
        CalibrateFirst = "calibrate first: click ⚙ calibrate panel region",
        CaptureError = "capture error: {0}",
        ReadStatus = "read: {0} bids / {1} asks",
        MissingSide = "read ({0}): {1} bids / {2} asks — still need the {3} side",
        MissingBids = "sell (bids)",
        MissingAsks = "buy (asks)",
        NoteDust = "skipped dust order",
        NoteThin = "thin top of book",
        NoteSuspect = "suspect reading, verify",
        RegionSaved = "region saved: {0} — Num4/Num5 capture",
        HotkeysFailed = "⚠ hotkeys not registered: {0} (conflict? edit config.json)",

        WarnSpreadClosed = "✖ Spread closed — don't flip now, you'd lose money.",
        WarnTightMargin = "⚠ Margin below 0.7% — gold fees will likely eat the profit.",

        TrayShowHide = "Show / hide",
        TrayInteractive = "Interactive mode (move)",
        TrayCalibrate = "Calibrate panel region…",
        TrayLanguage = "Português",
        TrayQuit = "Quit",

        CalibInstruction = "Draw ONE rectangle covering the currency NAMES and the ratio list · Esc cancels",

        AlreadyOpen =
            "PoE2 Flip Overlay is already running.\n\nClose the current instance (tray → Quit, or Ctrl+Shift+X) before opening again.",
    };
}
