using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace POE2FlipTool.Modules
{
    /// <summary>One trading pair in one league for one hour, as reported by GGG's currency exchange digest.</summary>
    public class MarketVolume
    {
        public string League { get; set; } = "";
        public string ItemA { get; set; } = "";
        public string ItemB { get; set; } = "";
        /// <summary>Units of ItemA that changed hands during the hour.</summary>
        public double VolumeA { get; set; }
        /// <summary>Units of ItemB that changed hands during the hour.</summary>
        public double VolumeB { get; set; }
    }

    /// <summary>
    /// Fetches GGG's public hourly currency-exchange digest
    /// (https://web.poecdn.com/api/currency-exchange[/poe2]/&lt;hour&gt;) at most once per hour and caches it
    /// in cache/(poe1|poe2)/exchange_volume.json. Every league is kept so switching the league dropdown needs no refetch.
    /// </summary>
    public class ExchangeVolumeService
    {
        public static readonly TimeSpan MIN_INTERVAL = TimeSpan.FromHours(1);

        private readonly string _poeConfig;
        private readonly string _cachePath;

        private CacheFile _cache = new CacheFile();
        private Dictionary<(string League, string A, string B), MarketVolume> _index = new();

        private sealed class CacheFile
        {
            /// <summary>When the API was last called (success or failure). Drives the once-per-hour rule.</summary>
            public DateTime? LastAttemptUtc { get; set; }
            /// <summary>Unix timestamp of the hour the cached markets describe.</summary>
            public long HourId { get; set; }
            public List<MarketVolume> Markets { get; set; } = new List<MarketVolume>();
        }

        public ExchangeVolumeService(string poeConfig)
        {
            _poeConfig = poeConfig;
            _cachePath = Path.Combine(PoeHttp.CacheDirectory(poeConfig), "exchange_volume.json");
            LoadCache();
        }

        public DateTime? LastAttemptUtc => _cache.LastAttemptUtc;

        /// <summary>Start of the hour the cached data describes, in local time, or null when nothing is cached.</summary>
        public DateTime? DataHourLocal => _cache.HourId == 0
            ? null
            : DateTimeOffset.FromUnixTimeSeconds(_cache.HourId).LocalDateTime;

        public bool HasData => _cache.Markets.Count > 0;

        public bool IsRefreshDue()
        {
            return _cache.LastAttemptUtc == null || DateTime.UtcNow - _cache.LastAttemptUtc.Value >= MIN_INTERVAL;
        }

        /// <summary>
        /// Calls the API if the last attempt was an hour or more ago. Returns true when new data was stored.
        /// A failed call still counts as an attempt, so the API is never hit more than once per hour.
        /// </summary>
        public async Task<bool> RefreshIfDueAsync()
        {
            if (!IsRefreshDue()) return false;

            _cache.LastAttemptUtc = DateTime.UtcNow;
            try
            {
                long lastClosedHour = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 3600 * 3600 - 3600;

                // The digest for the hour that just closed can lag a little; fall back one more hour if it is still empty.
                List<MarketVolume>? markets = await FetchHourAsync(lastClosedHour);
                long hourId = lastClosedHour;
                if (markets == null || markets.Count == 0)
                {
                    hourId = lastClosedHour - 3600;
                    markets = await FetchHourAsync(hourId);
                }

                if (markets != null && markets.Count > 0)
                {
                    _cache.HourId = hourId;
                    _cache.Markets = markets;
                    RebuildIndex();
                }
                SaveCache();
                return markets != null && markets.Count > 0;
            }
            catch (Exception)
            {
                SaveCache(); // keep the attempt timestamp even when the call failed
                return false;
            }
        }

        private async Task<List<MarketVolume>?> FetchHourAsync(long hourId)
        {
            string url = "https://web.poecdn.com/api/currency-exchange/"
                       + (_poeConfig == "poe2" ? "poe2/" : "")
                       + hourId.ToString(CultureInfo.InvariantCulture);

            string json = await PoeHttp.Client.GetStringAsync(url);
            using JsonDocument doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("markets", out JsonElement markets)) return null;

            var result = new List<MarketVolume>();
            foreach (JsonElement m in markets.EnumerateArray())
            {
                if (!m.TryGetProperty("market_pair", out JsonElement pair) || pair.GetArrayLength() != 2) continue;
                string a = pair[0].GetString() ?? "";
                string b = pair[1].GetString() ?? "";
                if (a.Length == 0 || b.Length == 0) continue;

                var mv = new MarketVolume
                {
                    League = m.TryGetProperty("league", out JsonElement l) ? l.GetString() ?? "" : "",
                    ItemA = a,
                    ItemB = b,
                };
                if (m.TryGetProperty("volume_traded", out JsonElement vol))
                {
                    if (vol.TryGetProperty(a, out JsonElement va)) mv.VolumeA = va.GetDouble();
                    if (vol.TryGetProperty(b, out JsonElement vb)) mv.VolumeB = vb.GetDouble();
                }
                result.Add(mv);
            }
            return result;
        }

        /// <summary>Every metadata id that appears in the cached digest, across all leagues.</summary>
        public IEnumerable<string> AllItemIds()
        {
            return _cache.Markets.SelectMany(m => new[] { m.ItemA, m.ItemB }).Distinct();
        }

        /// <summary>Leagues present in the cached digest, busiest first.</summary>
        public List<string> Leagues()
        {
            return _cache.Markets.GroupBy(m => m.League).OrderByDescending(g => g.Count()).Select(g => g.Key).ToList();
        }

        /// <summary>
        /// Volume of the item &lt;-&gt; currency market in a league during the cached hour:
        /// how many items and how many units of the currency were traded. Null when the pair had no trades.
        /// </summary>
        public (double Items, double Currency)? GetPairVolume(string league, string itemId, string currencyId)
        {
            if (_index.TryGetValue((league, itemId, currencyId), out MarketVolume? m))
            {
                return (m.VolumeA, m.VolumeB);
            }
            if (_index.TryGetValue((league, currencyId, itemId), out m))
            {
                return (m.VolumeB, m.VolumeA);
            }
            return null;
        }

        private void RebuildIndex()
        {
            _index = new Dictionary<(string, string, string), MarketVolume>();
            foreach (var m in _cache.Markets)
            {
                _index[(m.League, m.ItemA, m.ItemB)] = m;
            }
        }

        private static readonly JsonSerializerOptions JSON = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private void LoadCache()
        {
            try
            {
                if (File.Exists(_cachePath))
                {
                    _cache = JsonSerializer.Deserialize<CacheFile>(File.ReadAllText(_cachePath), JSON) ?? new CacheFile();
                    RebuildIndex();
                }
            }
            catch (Exception)
            {
                _cache = new CacheFile();
            }
        }

        private void SaveCache()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_cachePath)!);
                File.WriteAllText(_cachePath, JsonSerializer.Serialize(_cache, JSON));
            }
            catch (Exception)
            {
                // a failed cache write only costs an extra API call next start
            }
        }
    }
}
