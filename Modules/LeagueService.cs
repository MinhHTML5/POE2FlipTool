using System.Net.Http;
using System.Text.Json;

namespace POE2FlipTool.Modules
{
    /// <summary>
    /// Fetches the list of currently running leagues for a game from GGG's public endpoints.
    /// Primary source: the trade site's league list (no token needed, current challenge league first).
    /// Fallback: the league names that appear in the last closed hour of the currency exchange digest.
    /// </summary>
    public class LeagueService
    {
        private static HttpClient Http => PoeHttp.Client;

        /// <summary>"poe2" -> PoE2 realm, anything else -> PoE1 PC realm.</summary>
        public static string RealmFor(string poeConfig) => poeConfig == "poe2" ? "poe2" : "pc";

        public async Task<List<string>> GetLeaguesAsync(string poeConfig)
        {
            try
            {
                List<string> leagues = await GetTradeSiteLeaguesAsync(poeConfig);
                if (leagues.Count > 0) return leagues;
            }
            catch (Exception)
            {
                // fall through to the exchange digest
            }
            return await GetLeaguesFromExchangeDigestAsync(poeConfig);
        }

        /// <summary>
        /// https://www.pathofexile.com/api/trade/data/leagues (PoE1) or /api/trade2/data/leagues (PoE2).
        /// Returns { "result": [ { "id", "realm", "text" }, ... ] }; PoE1 also lists console realms, which are skipped.
        /// </summary>
        public async Task<List<string>> GetTradeSiteLeaguesAsync(string poeConfig)
        {
            string url = poeConfig == "poe2"
                ? "https://www.pathofexile.com/api/trade2/data/leagues"
                : "https://www.pathofexile.com/api/trade/data/leagues";
            string realm = RealmFor(poeConfig);

            string json = await Http.GetStringAsync(url);
            using JsonDocument doc = JsonDocument.Parse(json);

            var result = new List<string>();
            if (!doc.RootElement.TryGetProperty("result", out JsonElement list)) return result;

            foreach (JsonElement entry in list.EnumerateArray())
            {
                string? entryRealm = entry.TryGetProperty("realm", out JsonElement r) ? r.GetString() : null;
                string? id = entry.TryGetProperty("id", out JsonElement i) ? i.GetString() : null;
                if (entryRealm == realm && !string.IsNullOrEmpty(id) && !result.Contains(id))
                {
                    result.Add(id);
                }
            }
            return result;
        }

        /// <summary>
        /// https://web.poecdn.com/api/currency-exchange[/poe2]/&lt;hour&gt;: every market row names its league.
        /// Leagues are returned busiest first, so the current challenge league comes out on top.
        /// </summary>
        public async Task<List<string>> GetLeaguesFromExchangeDigestAsync(string poeConfig)
        {
            long lastClosedHour = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 3600 * 3600 - 3600;
            string url = "https://web.poecdn.com/api/currency-exchange/"
                       + (poeConfig == "poe2" ? "poe2/" : "")
                       + lastClosedHour;

            string json = await Http.GetStringAsync(url);
            using JsonDocument doc = JsonDocument.Parse(json);

            var counts = new Dictionary<string, int>();
            if (doc.RootElement.TryGetProperty("markets", out JsonElement markets))
            {
                foreach (JsonElement market in markets.EnumerateArray())
                {
                    if (market.TryGetProperty("league", out JsonElement l) && l.GetString() is string league)
                    {
                        counts[league] = counts.TryGetValue(league, out int n) ? n + 1 : 1;
                    }
                }
            }

            return counts.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToList();
        }
    }
}
