using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace ninja.poe;

internal sealed class ApiHelper
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiHelper(HttpClient http)
    {
        _http = http;
    }

    public async Task<T> GetAsync<T>(string url)
    {
        var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;
    }
}
