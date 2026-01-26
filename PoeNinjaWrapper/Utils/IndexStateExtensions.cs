namespace ninja.poe;

public static class IndexStateExtensions
{
    public static EconomyLeague GetDefaultLeague(this IndexState state)
    {
        if (state.EconomyLeagues.Count == 0)
            throw new InvalidOperationException("No economy leagues found.");

        return state.EconomyLeagues[0];
    }
}
