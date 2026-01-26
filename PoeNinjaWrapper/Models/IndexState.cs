using System.Text.Json.Serialization;

namespace ninja.poe;

public sealed class IndexState
{
    [JsonPropertyName("economyLeagues")]
    public List<EconomyLeague> EconomyLeagues { get; set; } = [];

    [JsonPropertyName("oldEconomyLeagues")]
    public List<EconomyLeague> OldEconomyLeagues { get; set; } = [];

    [JsonPropertyName("snapshotVersions")]
    public List<SnapshotVersion> SnapshotVersions { get; set; } = [];

    [JsonPropertyName("buildLeagues")]
    public List<EconomyLeague> BuildLeagues { get; set; } = [];

    [JsonPropertyName("oldBuildLeagues")]
    public List<EconomyLeague> OldBuildLeagues { get; set; } = [];
}

public sealed class EconomyLeague
{
    public string Name { get; set; } = "";

    public string Url { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public bool Hardcore { get; set; }

    public bool Indexed { get; set; }
}

public sealed class SnapshotVersion
{
    public string Url { get; set; } = "";

    public string Name { get; set; } = "";

    public List<string> TimeMachineLabels { get; set; } = [];

    public string Version { get; set; } = "";

    public string SnapshotName { get; set; } = "";

    public int OverviewType { get; set; }

    public string PassiveTree { get; set; } = "";
}