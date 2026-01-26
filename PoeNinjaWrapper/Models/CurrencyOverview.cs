using System.Text.Json.Serialization;

namespace ninja.poe;

public sealed class CurrencyOverview
{
    [JsonPropertyName("core")]
    public CurrencyCore Core { get; set; } = new();

    [JsonPropertyName("lines")]
    public List<CurrencyLine> Lines { get; set; } = [];

    [JsonPropertyName("items")]
    public List<CurrencyItem> Items { get; set; } = [];
}

public sealed class CurrencyCore
{
    public List<CoreItem> Items { get; set; } = [];

    public Dictionary<string, decimal> Rates { get; set; } = [];

    public string Primary { get; set; } = "";

    public string Secondary { get; set; } = "";
}
public sealed class CoreItem
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public string Image { get; set; } = "";

    public string Category { get; set; } = "";

    public string DetailsId { get; set; } = "";
}

public sealed class CurrencyLine
{
    public string Id { get; set; } = "";

    public decimal PrimaryValue { get; set; }

    public decimal VolumePrimaryValue { get; set; }

    public string MaxVolumeCurrency { get; set; } = "";

    public decimal MaxVolumeRate { get; set; }

    public SparkLine Sparkline { get; set; } = new();
}

public sealed class SparkLine
{
    public decimal? TotalChange { get; set; }

    public List<decimal?> Data { get; set; } = [];
}

public sealed class CurrencyItem
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public string Image { get; set; } = "";

    public string Category { get; set; } = "";

    public string DetailsId { get; set; } = "";
}