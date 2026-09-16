using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace POE2FlipTool.Modules
{
    /// <summary>
    /// Translates GGG metadata ids (e.g. "Metadata/Items/Currency/CurrencyUpgradeRandomly") into display names
    /// ("Orb of Chance") and back. Translations are cached in cache/(poe1|poe2)/item_names.json and only looked
    /// up again for ids that have no record yet (or whose last failed lookup is older than <see cref="RETRY_UNRESOLVED"/>).
    ///
    /// Sources, in order:
    ///  1. poe.ninja exchange overview: item names plus icon paths whose file stem usually equals the id's last segment.
    ///     Only unambiguous stems are trusted (tiers such as Exalted / Greater Exalted share one icon).
    ///  2. RePoE base_items dump: a direct id -> name table covering every base item of the game.
    /// </summary>
    public class ItemNameResolver
    {
        public static readonly TimeSpan RETRY_UNRESOLVED = TimeSpan.FromHours(24);

        private static readonly string[] POE1_NINJA_TYPES =
        {
            "Currency", "Fragment", "Runegraft", "AllflameEmber", "Tattoo", "Omen", "DjinnCoin", "Ducat",
            "EnshroudingCrystal", "DivinationCard", "Artifact", "Oil", "DeliriumOrb", "Scarab", "Astrolabe",
            "Fossil", "Resonator", "Essence"
        };
        private static readonly string[] POE2_NINJA_TYPES =
        {
            "Currency", "Fragments", "Abyss", "UncutGems", "LineageSupportGems", "Essences", "SoulCores",
            "Idols", "Runes", "Ritual", "Expedition", "Delirium", "Breach", "Verisium"
        };

        private readonly string _poeConfig;
        private readonly string _cachePath;
        private CacheFile _cache = new CacheFile();
        private Dictionary<string, string> _idByName = new Dictionary<string, string>();

        private sealed class CacheFile
        {
            public Dictionary<string, string> Names { get; set; } = new Dictionary<string, string>();
            /// <summary>Ids no source could translate, with the time of the last attempt.</summary>
            public Dictionary<string, DateTime> Unresolved { get; set; } = new Dictionary<string, DateTime>();
        }

        public ItemNameResolver(string poeConfig)
        {
            _poeConfig = poeConfig;
            _cachePath = Path.Combine(PoeHttp.CacheDirectory(poeConfig), "item_names.json");
            LoadCache();
        }

        public int KnownCount => _cache.Names.Count;

        public string? GetName(string metadataId)
        {
            return _cache.Names.TryGetValue(metadataId, out string? name) ? name : null;
        }

        /// <summary>Metadata id for a display name as written in the sheet. Case and repeated spaces are ignored.</summary>
        public string? TryGetId(string displayName)
        {
            return _idByName.TryGetValue(NormalizeName(displayName), out string? id) ? id : null;
        }

        /// <summary>
        /// Looks up every id that has no translation record. Does nothing (and no network calls) when all ids are known.
        /// <paramref name="preferredIds"/> (ids seen in the exchange digest) win when several ids share one name.
        /// </summary>
        public async Task ResolveMissingAsync(IEnumerable<string> ids, string? league)
        {
            var idList = ids.Distinct().ToList();
            DateTime now = DateTime.UtcNow;
            var missing = idList
                .Where(id => !_cache.Names.ContainsKey(id))
                .Where(id => !_cache.Unresolved.TryGetValue(id, out DateTime tried) || now - tried >= RETRY_UNRESOLVED)
                .ToList();

            if (missing.Count > 0)
            {
                var stillMissing = new HashSet<string>(missing);

                if (!string.IsNullOrEmpty(league))
                {
                    try { await ResolveFromPoeNinjaAsync(stillMissing, league); }
                    catch (Exception) { /* fall through to the next source */ }
                }

                if (stillMissing.Count > 0)
                {
                    try { await ResolveFromRePoEAsync(stillMissing); }
                    catch (Exception) { /* leave the rest unresolved */ }
                }

                foreach (string id in stillMissing)
                {
                    _cache.Unresolved[id] = now;
                }
                SaveCache();
            }

            RebuildReverseIndex(idList);
        }

        // ------------------------------------------------------------------
        // Source 1: poe.ninja
        // ------------------------------------------------------------------

        private async Task ResolveFromPoeNinjaAsync(HashSet<string> missing, string league)
        {
            string game = _poeConfig == "poe2" ? "poe2" : "poe1";
            string[] types = _poeConfig == "poe2" ? POE2_NINJA_TYPES : POE1_NINJA_TYPES;

            // icon file stem -> set of names using that icon
            var namesByStem = new Dictionary<string, HashSet<string>>();
            foreach (string type in types)
            {
                string url = $"https://poe.ninja/{game}/api/economy/exchange/current/overview?league={Uri.EscapeDataString(league)}&type={type}";
                string json;
                try { json = await PoeHttp.Client.GetStringAsync(url); }
                catch (Exception) { continue; }

                using JsonDocument doc = JsonDocument.Parse(json);
                foreach (JsonElement item in EnumerateNinjaItems(doc.RootElement))
                {
                    string? name = item.TryGetProperty("name", out JsonElement n) ? n.GetString() : null;
                    string? image = item.TryGetProperty("image", out JsonElement im) ? im.GetString() : null;
                    string? stem = IconStem(image);
                    if (string.IsNullOrEmpty(name) || stem == null) continue;

                    if (!namesByStem.TryGetValue(stem, out HashSet<string>? set))
                    {
                        set = new HashSet<string>();
                        namesByStem[stem] = set;
                    }
                    set.Add(name);
                }
            }

            foreach (string id in missing.ToList())
            {
                string lastSegment = id.Substring(id.LastIndexOf('/') + 1);
                if (namesByStem.TryGetValue(lastSegment, out HashSet<string>? names) && names.Count == 1)
                {
                    _cache.Names[id] = names.First();
                    _cache.Unresolved.Remove(id);
                    missing.Remove(id);
                }
            }
        }

        private static IEnumerable<JsonElement> EnumerateNinjaItems(JsonElement root)
        {
            if (root.TryGetProperty("items", out JsonElement items))
            {
                foreach (JsonElement e in items.EnumerateArray()) yield return e;
            }
            if (root.TryGetProperty("core", out JsonElement core) && core.TryGetProperty("items", out JsonElement coreItems))
            {
                foreach (JsonElement e in coreItems.EnumerateArray()) yield return e;
            }
        }

        /// <summary>
        /// poe.ninja icon urls look like /gen/image/&lt;base64 json&gt;/&lt;hash&gt;/CurrencyModValues.png; the base64 part
        /// holds the art path ("2DItems/Currency/CurrencyModValues"). The file stem is what we match on.
        /// </summary>
        private static string? IconStem(string? image)
        {
            if (string.IsNullOrEmpty(image)) return null;

            Match m = Regex.Match(image, @"/gen/image/([A-Za-z0-9_\-]+)/");
            if (m.Success)
            {
                try
                {
                    string b64 = m.Groups[1].Value.Replace('-', '+').Replace('_', '/');
                    b64 = b64.PadRight(b64.Length + (4 - b64.Length % 4) % 4, '=');
                    using JsonDocument doc = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(b64)));
                    if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 2
                        && doc.RootElement[2].TryGetProperty("f", out JsonElement f) && f.GetString() is string art)
                    {
                        return art.Substring(art.LastIndexOf('/') + 1);
                    }
                }
                catch (Exception) { /* fall back to the file name */ }
            }

            string file = image.Substring(image.LastIndexOf('/') + 1);
            int dot = file.LastIndexOf('.');
            return dot > 0 ? file.Substring(0, dot) : file;
        }

        // ------------------------------------------------------------------
        // Source 2: RePoE base item dump
        // ------------------------------------------------------------------

        private async Task ResolveFromRePoEAsync(HashSet<string> missing)
        {
            string url = _poeConfig == "poe2"
                ? "https://repoe-fork.github.io/poe2/base_items.min.json"
                : "https://repoe-fork.github.io/base_items.min.json";

            string json = await PoeHttp.Client.GetStringAsync(url);
            using JsonDocument doc = JsonDocument.Parse(json);

            foreach (string id in missing.ToList())
            {
                if (doc.RootElement.TryGetProperty(id, out JsonElement entry)
                    && entry.TryGetProperty("name", out JsonElement n)
                    && n.GetString() is string name && name.Length > 0)
                {
                    _cache.Names[id] = name;
                    _cache.Unresolved.Remove(id);
                    missing.Remove(id);
                }
            }
        }

        // ------------------------------------------------------------------
        // Reverse index and cache
        // ------------------------------------------------------------------

        private void RebuildReverseIndex(List<string> preferredIds)
        {
            var preferred = new HashSet<string>(preferredIds);
            _idByName = new Dictionary<string, string>();
            foreach (var (id, name) in _cache.Names)
            {
                string key = NormalizeName(name);
                if (!_idByName.TryGetValue(key, out string? existing) || (!preferred.Contains(existing) && preferred.Contains(id)))
                {
                    _idByName[key] = id;
                }
            }
        }

        public static string NormalizeName(string name)
        {
            return Regex.Replace(name.Trim(), @"\s+", " ").ToLowerInvariant();
        }

        private void LoadCache()
        {
            try
            {
                if (File.Exists(_cachePath))
                {
                    _cache = JsonSerializer.Deserialize<CacheFile>(File.ReadAllText(_cachePath)) ?? new CacheFile();
                }
            }
            catch (Exception)
            {
                _cache = new CacheFile();
            }
            RebuildReverseIndex(new List<string>());
        }

        private void SaveCache()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_cachePath)!);
                File.WriteAllText(_cachePath, JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception)
            {
                // a failed cache write only means another lookup next start
            }
        }
    }
}
