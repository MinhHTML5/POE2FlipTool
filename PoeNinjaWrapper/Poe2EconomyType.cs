namespace ninja.poe;

[Flags]
public enum Poe2EconomyType
{
    None = 0,

    Currency = 1 << 0,
    Fragments = 1 << 1,
    Abyss = 1 << 2,
    UncutGems = 1 << 3,
    LineageSupportGems = 1 << 4,
    Essences = 1 << 5,
    SoulCores = 1 << 6,
    Idols = 1 << 7,
    Runes = 1 << 8,
    Ritual = 1 << 9,
    Expedition = 1 << 10,
    Delirium = 1 << 11,
    Breach = 1 << 12,

    All = ~0
}
