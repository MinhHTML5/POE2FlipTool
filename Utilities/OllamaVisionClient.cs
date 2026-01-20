using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public sealed class OllamaVisionClient : IDisposable
{
    private readonly HttpClient _http;
    //string baseUrl = "http://localhost:11434"

    private static OllamaVisionClient _instance;
    public static OllamaVisionClient Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new OllamaVisionClient();
            }
            return _instance;
        }
    }

    private OllamaVisionClient(string baseUrl = "http://192.168.0.110:11434")
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(60)
        };
    }

    // =========================
    // PUBLIC SYNC API (YOU USE THIS)
    // =========================
    public string Send(Bitmap bitmap, string prompt)
    {
        return SendAsync(bitmap, prompt)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    // =========================
    // INTERNAL ASYNC IMPLEMENTATION
    // =========================
    private async Task<string> SendAsync(Bitmap bitmap, string prompt)
    {
        string imageBase64 = BitmapToBase64(bitmap);

        var payload = new
        {
            model = "chevalblanc/gpt-4o-mini",
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt,
                    images = new[] { imageBase64 }
                }
            },
            stream = false
        };

        string json = JsonSerializer.Serialize(payload);

        using var response = await _http.PostAsync(
            "/api/chat",
            new StringContent(json, Encoding.UTF8, "application/json")
        ).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        string responseJson = await response.Content
            .ReadAsStringAsync()
            .ConfigureAwait(false);

        using JsonDocument doc = JsonDocument.Parse(responseJson);

        return doc.RootElement.GetProperty("message").GetProperty("content").GetString();
    }

    private static string BitmapToBase64(Bitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png); // PNG = best for digits
        return Convert.ToBase64String(ms.ToArray());
    }

    public void Dispose()
    {
        _http.Dispose();
    }
}
