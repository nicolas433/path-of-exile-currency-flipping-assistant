using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PoE2FlipOverlay.App;

/// <summary>One remembered reading for a currency pair.</summary>
public sealed class HistoryEntry
{
    public string Pair { get; set; } = "";
    public decimal BestBid { get; set; }
    public decimal BestAsk { get; set; }
    public decimal Margin { get; set; }
    public decimal Profit { get; set; }
    public DateTime CheckedAt { get; set; }

    // Display helpers (not persisted).
    [JsonIgnore] public string BidAskText => $"{Fmt(BestBid)} / {Fmt(BestAsk)}";
    [JsonIgnore] public string MarginText => $"{Fmt(Margin)}%";
    [JsonIgnore] public string TimeText => CheckedAt.ToString("HH:mm", CultureInfo.InvariantCulture);

    private static string Fmt(decimal v) => v.ToString("0.##", CultureInfo.InvariantCulture);
}

/// <summary>
/// Keeps the most recent reading per currency pair, newest first, persisted to
/// <c>history.json</c> next to the executable.
/// </summary>
public sealed class HistoryStore
{
    private const int MaxEntries = 10;

    private static string FilePath => Path.Combine(AppContext.BaseDirectory, "history.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public ObservableCollection<HistoryEntry> Entries { get; } = new();

    public void Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return;
            var list = JsonSerializer.Deserialize<List<HistoryEntry>>(File.ReadAllText(FilePath));
            if (list is null) return;

            Entries.Clear();
            foreach (var e in list) Entries.Add(e);
        }
        catch
        {
            // Ignore a corrupt history file.
        }
    }

    /// <summary>Records the latest reading for a pair, replacing any previous one.</summary>
    public void Record(string pair, decimal bestBid, decimal bestAsk, decimal margin, decimal profit)
    {
        for (int i = Entries.Count - 1; i >= 0; i--)
            if (string.Equals(Entries[i].Pair, pair, StringComparison.OrdinalIgnoreCase))
                Entries.RemoveAt(i);

        Entries.Insert(0, new HistoryEntry
        {
            Pair = pair,
            BestBid = bestBid,
            BestAsk = bestAsk,
            Margin = margin,
            Profit = profit,
            CheckedAt = DateTime.Now
        });

        while (Entries.Count > MaxEntries)
            Entries.RemoveAt(Entries.Count - 1);

        Save();
    }

    private void Save()
    {
        try
        {
            File.WriteAllText(FilePath, JsonSerializer.Serialize(Entries.ToList(), JsonOptions));
        }
        catch
        {
            // Best effort.
        }
    }
}
