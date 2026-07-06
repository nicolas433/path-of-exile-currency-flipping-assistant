namespace PoE2FlipOverlay.Core;

/// <summary>
/// Detects which currencies a block of OCR text refers to, by matching against
/// a list of known PoE2 currency names. Returns them in order of appearance,
/// so the first is the "I Want" side and the second the "I Have" side.
/// </summary>
public static class CurrencyNames
{
    // Common tradeable currencies. Longer names first so "Mirror of Kalandra"
    // wins over a bare "Mirror". Extend as needed.
    public static readonly IReadOnlyList<string> Known = new[]
    {
        "Mirror of Kalandra",
        "Fracturing Orb",
        "Divine Orb",
        "Chaos Orb",
        "Exalted Orb",
        "Orb of Annulment",
        "Orb of Alchemy",
        "Orb of Chance",
        "Vaal Orb",
        "Regal Orb",
        "Gemcutter's Prism",
        "Glassblower's Bauble",
        "Artificer's Orb",
        "Chromatic Orb",
        "Blacksmith's Whetstone",
        "Armourer's Scrap",
        "Perfect Jeweller's Orb",
        "Greater Jeweller's Orb",
        "Lesser Jeweller's Orb",
    };

    /// <summary>
    /// Returns the first two distinct known currencies found in
    /// <paramref name="text"/>, in order of appearance.
    /// </summary>
    public static (string? First, string? Second) DetectPair(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (null, null);

        var hits = new List<(int Index, string Name)>();
        foreach (var name in Known)
        {
            int i = text.IndexOf(name, StringComparison.OrdinalIgnoreCase);
            if (i >= 0 && hits.All(h => h.Name != name))
                hits.Add((i, name));
        }

        var ordered = hits.OrderBy(h => h.Index).Select(h => h.Name).ToList();
        return (
            ordered.Count > 0 ? ordered[0] : null,
            ordered.Count > 1 ? ordered[1] : null);
    }
}
