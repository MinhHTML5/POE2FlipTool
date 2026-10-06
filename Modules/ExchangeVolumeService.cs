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
        /// <summary>GGG "lowest_ratio": LowA units of ItemA for LowB units of ItemB.</summary>
        public double LowA { get; set; }
        public double LowB { get; set; }
        /// <summary>GGG "highest_ratio": HighA units of ItemA for HighB units of ItemB.</summary>
        public double HighA { get; set; }
        public double HighB { get; set; }

        public bool Involves(string id) => ItemA == id || ItemB == id;

        public double VolumeOf(string id) => id == ItemA ? VolumeA : VolumeB;

        /// <summary>
        /// Price of one <paramref name="itemId"/> expressed in the other side of the pair, as the (min, max)
        /// seen during the hour. Null when either ratio is missing.
        /// </summary>
        public (double Min, double Max)? PricePerItem(string itemId)
        {
            if (LowA <= 0 || LowB <= 0 || HighA <= 0 || HighB <= 0) return null;
            double p1, p2;
            if (itemId == ItemA)
            {
                p1 = LowB / LowA;
                p2 = HighB / HighA;
            }
            else
            {
                p1 = LowA / LowB;
                p2 = HighA / HighB;
            }
            return (Math.Min(p1, p2), Math.Max(p1, p2));
        }
    }

    /// <summary>One hour of one item/currency pair, ready for charting.</summary>
    public class HourlyPricePoint
    {
        public long HourId { get; set; }
        public DateTime HourStartLocal { get; set; }
        public double MinPrice { get; set; }
        public double MaxPrice { get; set; }
        public double ItemVolume { get; set; }
        public double CurrencyVolume { get; set; }
    }

    /// <summary>
    /// Fetches GGG's public hourly currency-exchange digest
    /// (https://web.poecdn.com/api/currency-exchange[/poe2]/&lt;hour&gt;) and caches one file per hour in
    /// cache/(poe1|poe2)/exchange/&lt;hour&gt;.json, every league included.
    ///
    /// Throttling: a refresh runs at most once per <see cref="MIN_INTERVAL"/> (the attempt time is persisted).
    /// When it runs it backfills every closed hour after the last cached one, capped at the most recent
    /// <see cref="MaxBackfillHours"/>, so a restart after a long pause catches up on up to a day of data.
    /// </summary>
    public class ExchangeVolumeService
    {
        public static readonly TimeSpan MIN_INTERVAL = TimeSpan.FromHours(1);
        public const int DEFAULT_MAX_BACKFILL_HOURS = 24;
        public const int RETENTION_HOURS = 72;
        private const int CACHE_VERSION = 2;

        private readonly string _poeConfig;
        private readonly string _metaPath;
        private readonly string _hoursDir;

        private MetaFile _meta = new MetaFile();
        private readonly Dictionary<long, Dictionary<(string League, string A, string B), MarketVolume>> _hours = new();

        /// <summary>How many hours one refresh may fetch. 24 by default; tests lower it.</summary>
        public int MaxBackfillHours { get; set; } = DEFAULT_MAX_BACKFILL_HOURS;

        private sealed class MetaFile
        {
            public int Version { get; set; } = CACHE_VERSION;
            /// <summary>When the API was last called (success or failure). Drives the once-per-hour rule.</summary>
            public DateTime? LastAttemptUtc { get; set; }
            /// <summary>Unix timestamp of the newest hour that has been fetched.</summary>
            public long HourId { get; set; }
        }

        public ExchangeVolumeService(string poeConfig)
        {
            _poeConfig = poeConfig;
            _metaPath = Path.Combine(PoeHttp.CacheDirectory(poeConfig), "exchange_volume.json");
            _hoursDir = Path.Combine(PoeHttp.CacheDirectory(poeConfig), "exchange");
            LoadMeta();
        }

        public DateTime? LastAttemptUtc => _meta.LastAttemptUtc;

        /// <summary>Start of the newest cached hour, local time, or null when nothing is cached.</summary>
        public DateTime? DataHourLocal => _meta.HourId == 0 ? null : HourStartLocal(_meta.HourId);

        public bool HasData => _meta.HourId != 0 && File.Exists(HourPath(_meta.HourId));

        public bool IsRefreshDue()
        {
            return _meta.LastAttemptUtc == null || DateTime.UtcNow - _meta.LastAttemptUtc.Value >= MIN_INTERVAL;
        }

        /// <summary>
        /// Backfills every closed hour since the last cached one (capped at <see cref="MaxBackfillHours"/>),
        /// if the last attempt is at least an hour old. Returns true when at least one new hour was stored.
        /// A failed call still counts as an attempt, so the API is never hit more than once per hour.
        /// </summary>
        public async Task<bool> RefreshIfDueAsync()
        {
            if (!IsRefreshDue()) return false;

            _meta.LastAttemptUtc = DateTime.UtcNow;
            SaveMeta();

            bool storedAny = false;
            try
            {
                long lastClosed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 3600 * 3600 - 3600;
                long oldestWanted = lastClosed - (MaxBackfillHours - 1) * 3600L;
                long start = _meta.HourId > 0 ? _meta.HourId + 3600 : oldestWanted;
                if (start < oldestWanted) start = oldestWanted;

                for (long hour = start; hour <= lastClosed; hour += 3600)
                {
                    List<MarketVolume>? markets;
                    try { markets = await FetchHourAsync(hour); }
                    catch (Exception) { markets = null; }

                    if (markets == null || markets.Count == 0)
                    {
                        // The newest hour can lag publication; stop here and pick it up next time.
                        // An older empty/failed hour is skipped so one bad hour cannot stall the backfill.
                        if (hour == lastClosed) break;
                        continue;
                    }

                    SaveHour(hour, markets);
                    _meta.HourId = Math.Max(_meta.HourId, hour);
                    storedAny = true;
                }
            }
            finally
            {
                PruneOldHours();
                SaveMeta();
            }
            return storedAny;
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
                (mv.VolumeA, mv.VolumeB) = ReadPair(m, "volume_traded", a, b);
                (mv.LowA, mv.LowB) = ReadPair(m, "lowest_ratio", a, b);
                (mv.HighA, mv.HighB) = ReadPair(m, "highest_ratio", a, b);
                result.Add(mv);
            }
            return result;
        }

        private static (double, double) ReadPair(JsonElement market, string property, string a, string b)
        {
            if (!market.TryGetProperty(property, out JsonElement obj)) return (0, 0);
            double va = obj.TryGetProperty(a, out JsonElement ea) && ea.ValueKind == JsonValueKind.Number ? ea.GetDouble() : 0;
            double vb = obj.TryGetProperty(b, out JsonElement eb) && eb.ValueKind == JsonValueKind.Number ? eb.GetDouble() : 0;
            return (va, vb);
        }

        // ------------------------------------------------------------------
        // Queries
        // ------------------------------------------------------------------

        /// <summary>Every metadata id that appears in the newest cached hour, across all leagues.</summary>
        public IEnumerable<string> AllItemIds()
        {
            var latest = LoadHour(_meta.HourId);
            return latest == null ? Enumerable.Empty<string>() : latest.Values.SelectMany(m => new[] { m.ItemA, m.ItemB }).Distinct();
        }

        /// <summary>Leagues present in the newest cached hour, busiest first.</summary>
        public List<string> Leagues()
        {
            var latest = LoadHour(_meta.HourId);
            if (latest == null) return new List<string>();
            return latest.Values.GroupBy(m => m.League).OrderByDescending(g => g.Count()).Select(g => g.Key).ToList();
        }

        /// <summary>
        /// Volume of the item &lt;-&gt; currency market in a league during the newest cached hour:
        /// how many items and how many units of the currency were traded. Null when the pair had no trades.
        /// </summary>
        public (double Items, double Currency)? GetPairVolume(string league, string itemId, string currencyId)
        {
            MarketVolume? m = FindMarket(_meta.HourId, league, itemId, currencyId);
            return m == null ? null : (m.VolumeOf(itemId), m.VolumeOf(currencyId));
        }

        /// <summary>Hours (unix ids) cached whose local start time falls on <paramref name="dayLocal"/>, ascending.</summary>
        public List<long> HoursCachedOn(DateTime dayLocal)
        {
            DateTime day = dayLocal.Date;
            return CachedHourIds().Where(h => HourStartLocal(h).Date == day).OrderBy(h => h).ToList();
        }

        /// <summary>Cached hours of the day in which the league had at least one market (i.e. the league existed).</summary>
        public List<long> HoursWithLeagueOn(string league, DateTime dayLocal)
        {
            var result = new List<long>();
            foreach (long hour in HoursCachedOn(dayLocal))
            {
                var index = LoadHour(hour);
                if (index != null && index.Keys.Any(k => k.League == league)) result.Add(hour);
            }
            return result;
        }

        /// <summary>
        /// Min/max price of one item in one currency for every cached hour of a day where that pair traded.
        /// Hours before a league existed, or where the pair had no trades, simply have no point.
        /// </summary>
        public List<HourlyPricePoint> GetPairHistory(string league, string itemId, string currencyId, DateTime dayLocal)
        {
            var points = new List<HourlyPricePoint>();
            foreach (long hour in HoursCachedOn(dayLocal))
            {
                MarketVolume? m = FindMarket(hour, league, itemId, currencyId);
                var price = m?.PricePerItem(itemId);
                if (m == null || price == null) continue;

                points.Add(new HourlyPricePoint
                {
                    HourId = hour,
                    HourStartLocal = HourStartLocal(hour),
                    MinPrice = price.Value.Min,
                    MaxPrice = price.Value.Max,
                    ItemVolume = m.VolumeOf(itemId),
                    CurrencyVolume = m.VolumeOf(currencyId),
                });
            }
            return points;
        }

        public static DateTime HourStartLocal(long hourId) => DateTimeOffset.FromUnixTimeSeconds(hourId).LocalDateTime;

        private MarketVolume? FindMarket(long hourId, string league, string itemId, string currencyId)
        {
            var index = LoadHour(hourId);
            if (index == null) return null;
            if (index.TryGetValue((league, itemId, currencyId), out MarketVolume? m)) return m;
            if (index.TryGetValue((league, currencyId, itemId), out m)) return m;
            return null;
        }

        // ------------------------------------------------------------------
        // Cache files
        // ------------------------------------------------------------------

        private static readonly JsonSerializerOptions JSON = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private string HourPath(long hourId) => Path.Combine(_hoursDir, hourId.ToString(CultureInfo.InvariantCulture) + ".json");

        public List<long> CachedHourIds()
        {
            if (!Directory.Exists(_hoursDir)) return new List<long>();
            var ids = new List<long>();
            foreach (string file in Directory.EnumerateFiles(_hoursDir, "*.json"))
            {
                if (long.TryParse(Path.GetFileNameWithoutExtension(file), NumberStyles.Integer, CultureInfo.InvariantCulture, out long id))
                {
                    ids.Add(id);
                }
            }
            ids.Sort();
            return ids;
        }

        private Dictionary<(string League, string A, string B), MarketVolume>? LoadHour(long hourId)
        {
            if (hourId == 0) return null;
            if (_hours.TryGetValue(hourId, out var cached)) return cached;

            string path = HourPath(hourId);
            if (!File.Exists(path)) return null;
            try
            {
                var markets = JsonSerializer.Deserialize<List<MarketVolume>>(File.ReadAllText(path), JSON) ?? new List<MarketVolume>();
                var index = new Dictionary<(string, string, string), MarketVolume>();
                foreach (var m in markets) index[(m.League, m.ItemA, m.ItemB)] = m;
                _hours[hourId] = index;
                return index;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void SaveHour(long hourId, List<MarketVolume> markets)
        {
            Directory.CreateDirectory(_hoursDir);
            File.WriteAllText(HourPath(hourId), JsonSerializer.Serialize(markets, JSON));
            _hours.Remove(hourId);
        }

        private void PruneOldHours()
        {
            long cutoff = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - RETENTION_HOURS * 3600L;
            foreach (long hour in CachedHourIds().Where(h => h < cutoff))
            {
                try { File.Delete(HourPath(hour)); } catch (Exception) { }
                _hours.Remove(hour);
            }
        }

        private void LoadMeta()
        {
            try
            {
                if (File.Exists(_metaPath))
                {
                    var meta = JsonSerializer.Deserialize<MetaFile>(File.ReadAllText(_metaPath), JSON);
                    // Older cache layouts (single-hour file without ratios) are discarded and refetched.
                    if (meta != null && meta.Version == CACHE_VERSION) _meta = meta;
                }
            }
            catch (Exception)
            {
                _meta = new MetaFile();
            }

            // If the meta says we have an hour but its file is gone, start over from the backfill window.
            if (_meta.HourId != 0 && !File.Exists(HourPath(_meta.HourId)))
            {
                _meta.HourId = CachedHourIds().DefaultIfEmpty(0).Max();
            }
        }

        private void SaveMeta()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_metaPath)!);
                File.WriteAllText(_metaPath, JsonSerializer.Serialize(_meta, JSON));
            }
            catch (Exception)
            {
                // a failed cache write only costs an extra API call next start
            }
        }
    }
}
