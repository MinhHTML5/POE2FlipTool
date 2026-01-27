using POE2FlipTool.DataModel;

namespace ninja.poe;

public sealed class TradedItem
{
    public string Name { get; init; } = "";
    public decimal VolumePrimaryValue { get; init; }
    public Poe2EconomyType Poe2EconomyType { get; init; }

    public TradeItem ToTradeItem()
    {
        return new TradeItem(Poe2EconomyType.ToString(), Name);
    }
}
