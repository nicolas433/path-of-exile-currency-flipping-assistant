namespace PoE2FlipOverlay.App;

/// <summary>
/// A parsed global hotkey: Win32 modifier flags plus a virtual-key code.
/// Parses strings like "Ctrl+Shift+F", "Num4", "F8", "A".
/// </summary>
public readonly record struct HotkeyDefinition(uint Modifiers, uint VirtualKey, string Text)
{
    public static bool TryParse(string? spec, out HotkeyDefinition hotkey)
    {
        hotkey = default;
        if (string.IsNullOrWhiteSpace(spec)) return false;

        uint mods = 0, vk = 0;
        var parts = spec.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl":
                case "control": mods |= NativeMethods.MOD_CONTROL; break;
                case "alt": mods |= NativeMethods.MOD_ALT; break;
                case "shift": mods |= NativeMethods.MOD_SHIFT; break;
                case "win": mods |= NativeMethods.MOD_WIN; break;
                default:
                    if (!TryParseKey(part, out vk)) return false;
                    break;
            }
        }

        if (vk == 0) return false;

        // Don't autofire while the key is held down.
        mods |= NativeMethods.MOD_NOREPEAT;
        hotkey = new HotkeyDefinition(mods, vk, spec);
        return true;
    }

    private static bool TryParseKey(string key, out uint vk)
    {
        vk = 0;

        // Numpad: "Num0".."Num9" -> VK_NUMPAD0..9 (fires only with NumLock on).
        if (key.Length == 4 && key.StartsWith("Num", StringComparison.OrdinalIgnoreCase) && char.IsDigit(key[3]))
        {
            vk = 0x60u + (uint)(key[3] - '0');
            return true;
        }

        // Function keys "F1".."F12" -> VK_F1..F12.
        if (key.Length is 2 or 3 && (key[0] is 'F' or 'f') &&
            int.TryParse(key.AsSpan(1), out var fn) && fn is >= 1 and <= 12)
        {
            vk = 0x70u + (uint)(fn - 1);
            return true;
        }

        // Single letter A..Z (VK code == ASCII uppercase).
        if (key.Length == 1 && char.IsLetter(key[0]))
        {
            vk = char.ToUpperInvariant(key[0]);
            return true;
        }

        // Single top-row digit 0..9 (VK code == ASCII '0'..'9').
        if (key.Length == 1 && char.IsDigit(key[0]))
        {
            vk = key[0];
            return true;
        }

        return false;
    }
}
