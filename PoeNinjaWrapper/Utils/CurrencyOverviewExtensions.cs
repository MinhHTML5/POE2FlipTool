namespace ninja.poe;

public static class CurrencyOverviewExtensions
{
    // ---------------------------------------
    // Top traded (default top 20, all types)
    // ---------------------------------------
    public static IReadOnlyList<TradedItem> GetTopTradedItems(
        this IReadOnlyDictionary<Poe2EconomyType, CurrencyOverview> data,
        int top = 20,
        Poe2EconomyType types = Poe2EconomyType.All)
    {
        if (top <= 0)
            return Array.Empty<TradedItem>();

        return SelectLines(data, types)
            .OrderByDescending(x => x.Line.VolumePrimaryValue)
            .Take(top)
            .Select(ToTradedItem)
            .ToList();
    }


    // ---------------------------------------
    // All traded items
    // ---------------------------------------
    public static IReadOnlyList<TradedItem> GetAllTradedItems(
        this IReadOnlyDictionary<Poe2EconomyType, CurrencyOverview> data,
        Poe2EconomyType types = Poe2EconomyType.All)
    {
        return SelectLines(data, types)
            .OrderByDescending(x => x.Line.VolumePrimaryValue)
            .Select(ToTradedItem)
            .ToList();
    }

    // ---------------------------------------
    // Traded items above volume X
    // ---------------------------------------
    public static IReadOnlyList<TradedItem> GetTradedItemsAboveVolume(
        this IReadOnlyDictionary<Poe2EconomyType, CurrencyOverview> data,
        decimal minVolumePrimaryValue,
        Poe2EconomyType types = Poe2EconomyType.All)
    {
        return SelectLines(data, types)
            .Where(x => x.Line.VolumePrimaryValue > minVolumePrimaryValue)
            .OrderByDescending(x => x.Line.VolumePrimaryValue)
            .Select(ToTradedItem)
            .ToList();
    }
    public static TradedItem? GetMostTradedVolumeByType(
    this IReadOnlyDictionary<Poe2EconomyType, CurrencyOverview> data,
    Poe2EconomyType types = Poe2EconomyType.All)
    {
        return SelectLines(data, types)
            .OrderByDescending(x => x.Line.VolumePrimaryValue)
            .Select(ToTradedItem)
            .FirstOrDefault();
    }

    // =======================================
    // Internal helpers
    // =======================================

    private static IEnumerable<(Poe2EconomyType Type, CurrencyLine Line, CurrencyItem Item)> SelectLines(
        IReadOnlyDictionary<Poe2EconomyType, CurrencyOverview> data,
        Poe2EconomyType types)
    {
        foreach (var kv in data)
        {
            if (types != Poe2EconomyType.All &&
                (kv.Key & types) == 0)
                continue;

            var overview = kv.Value;
            var itemLookup = overview.Items.ToDictionary(i => i.Id);

            foreach (var line in overview.Lines)
            {
                if (!itemLookup.TryGetValue(line.Id, out var item))
                    continue;

                yield return (kv.Key, line, item);
            }
        }
    }
    private static TradedItem ToTradedItem(
        (Poe2EconomyType Type, CurrencyLine Line, CurrencyItem Item) x)
    {
        if (!Enum.TryParse<Poe2EconomyType>(
                x.Item.Category,
                ignoreCase: true,
                out var itemType))
        {
            itemType = Poe2EconomyType.None;
        }

        return new TradedItem
        {
            Name = x.Item.Name,
            VolumePrimaryValue = x.Line.VolumePrimaryValue,
            Poe2EconomyType = itemType
        };
    }
}
