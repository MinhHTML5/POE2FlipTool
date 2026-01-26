using System.Text.Json;

namespace ninja.poe;

public sealed class Poe2Ninja : IDisposable
{
    private const string BaseUrl = "https://poe.ninja/poe2/api/";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan FetchDelay = TimeSpan.FromSeconds(0.1);

    private readonly HttpClient _http;
    private readonly ApiHelper _api;
    private readonly string _cacheDir;

    private string? _cachedLeague;
    private readonly Dictionary<Poe2EconomyType, CurrencyOverview> _economy = new();
    private Action<string>? DebugLog { get; set; }


    public IReadOnlyDictionary<Poe2EconomyType, CurrencyOverview> Economy => _economy;

    public Poe2Ninja(Action<string>? debugLog = null, string? cacheDir = null)
    {
        DebugLog = debugLog;

        _cacheDir = cacheDir ??
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ninja.poe");

        Directory.CreateDirectory(_cacheDir);

        _http = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl)
        };

        _http.DefaultRequestHeaders.UserAgent.ParseAdd("ninja.poe/1.0");

        _api = new ApiHelper(_http);

        InitializeAsync().GetAwaiter().GetResult();
    }

    // ---------------- Initialization ----------------

    private async Task InitializeAsync()
    {
        if (IsCacheValid())
        {
            LoadFromDisk();
            return;
        }

        await LoadFromApiAsync();
        SaveMeta();
    }

    private bool IsCacheValid()
    {
        var metaPath = Path.Combine(_cacheDir, "meta.json");
        if (!File.Exists(metaPath))
            return false;

        var meta = JsonSerializer.Deserialize<CacheMeta>(
            File.ReadAllText(metaPath));

        return meta != null &&
               DateTimeOffset.UtcNow - meta.LastUpdatedUtc < CacheDuration;
    }

    // ---------------- League ----------------

    private async Task<string> GetCurrentLeagueAsync()
    {
        if (_cachedLeague != null)
            return _cachedLeague;

        var state = await _api.GetAsync<IndexState>("data/index-state");

        _cachedLeague = state.EconomyLeagues[0].Name;
        return _cachedLeague;
    }

    // ---------------- API Loading ----------------

    private async Task LoadFromApiAsync()
    {
        var league = await GetCurrentLeagueAsync();

        foreach (var type in Enum.GetValues<Poe2EconomyType>())
        {
            DebugLog?.Invoke($"[poe.ninja] GET {type}");

            var url =
                $"economy/exchange/current/overview" +
                $"?league={Uri.EscapeDataString(league)}" +
                $"&type={type}";
            var data = await _api.GetAsync<CurrencyOverview>(url);
            _economy[type] = data;

            File.WriteAllText(
                Path.Combine(_cacheDir, $"economy-{type}.json"),
                JsonSerializer.Serialize(data));

            await Task.Delay(FetchDelay);
        }
    }

    private void LoadFromDisk()
    {
        foreach (var type in Enum.GetValues<Poe2EconomyType>())
        {
            var path = Path.Combine(_cacheDir, $"economy-{type}.json");
            if (!File.Exists(path))
                continue;

            _economy[type] =
                JsonSerializer.Deserialize<CurrencyOverview>(
                    File.ReadAllText(path))!;
        }
    }

    private void SaveMeta()
    {
        var meta = new CacheMeta
        {
            LastUpdatedUtc = DateTimeOffset.UtcNow
        };

        File.WriteAllText(
            Path.Combine(_cacheDir, "meta.json"),
            JsonSerializer.Serialize(meta));
    }

    public void Dispose()
    {
        _http.Dispose();
    }
}
