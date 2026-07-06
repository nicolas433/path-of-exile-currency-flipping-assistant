using System.IO;
using System.Text.Json;

namespace PoE2FlipOverlay.App;

/// <summary>
/// User settings loaded from <c>config.json</c> next to the executable.
/// Written with defaults on first run so it is easy to hand-edit.
/// </summary>
public sealed class OverlayConfig
{
    public decimal Budget { get; set; } = 431m;
    public decimal Tick { get; set; } = 0.01m;
    public HotkeyConfig Hotkeys { get; set; } = new();

    /// <summary>
    /// Screen region (physical pixels) covering the exchange info area: the
    /// currency-name row plus the ratio list. Overridden by calibration.
    /// Default is pre-calibrated for 1920×1080 (Nicolas's setup).
    /// </summary>
    public CaptureRegion? Capture { get; set; } = new()
    {
        X = 567,
        Y = 108,
        Width = 787,
        Height = 268
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static string DefaultPath => Path.Combine(AppContext.BaseDirectory, "config.json");

    public static OverlayConfig LoadOrCreate(string? path = null)
    {
        path ??= DefaultPath;
        try
        {
            if (File.Exists(path))
            {
                var cfg = JsonSerializer.Deserialize<OverlayConfig>(File.ReadAllText(path), JsonOptions);
                if (cfg is not null) return cfg;
            }
        }
        catch
        {
            // Corrupt/unreadable config: fall back to defaults instead of crashing.
        }

        var defaults = new OverlayConfig();
        defaults.TrySave(path);
        return defaults;
    }

    public void TrySave(string? path = null)
    {
        try
        {
            File.WriteAllText(path ?? DefaultPath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch
        {
            // Best effort; running with in-memory defaults is fine.
        }
    }
}

/// <summary>
/// Hotkey bindings, one per action. Format: modifiers joined with '+', e.g.
/// "Ctrl+Shift+F", "Num4" (numpad 4, needs NumLock), "F8", "A".
/// </summary>
/// <summary>A rectangle on screen in physical pixels (what screen capture uses).</summary>
public sealed class CaptureRegion
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public override string ToString() => $"{Width}×{Height} @ ({X},{Y})";
}

public sealed class HotkeyConfig
{
    public string HideOverlay { get; set; } = "Num1";
    public string ShowOverlay { get; set; } = "Num2";
    public string CaptureBuy { get; set; } = "Num4";
    public string CaptureSell { get; set; } = "Num5";
    public string ToggleInteractive { get; set; } = "Ctrl+Shift+F";
    public string Quit { get; set; } = "Ctrl+Shift+X";
}
