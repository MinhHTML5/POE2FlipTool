namespace ninja.poe;

public sealed class TradedItem
{
    public string Name { get; init; } = "";
    public decimal VolumePrimaryValue { get; init; }
    public Poe2EconomyType Poe2EconomyType { get; init; }
}
