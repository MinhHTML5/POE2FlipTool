using System.Net.Http;

namespace POE2FlipTool.Modules
{
    /// <summary>One shared HttpClient for GGG and poe.ninja calls, with the User-Agent GGG asks clients to send.</summary>
    public static class PoeHttp
    {
        public const string USER_AGENT = "POE2FlipTool/1.0";

        public static readonly HttpClient Client = Create();

        private static HttpClient Create()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd(USER_AGENT);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            return client;
        }

        /// <summary>Metadata ids of the three reference currencies. Identical in PoE1 and PoE2.</summary>
        public const string DIVINE_ID = "Metadata/Items/Currency/CurrencyModValues";
        public const string EXALTED_ID = "Metadata/Items/Currency/CurrencyAddModToRare";
        public const string CHAOS_ID = "Metadata/Items/Currency/CurrencyRerollRare";

        /// <summary>Folder for cached API data: cache/(poe1|poe2)/ next to the executable.</summary>
        public static string CacheDirectory(string poeConfig) => Path.Combine("cache", poeConfig);
    }
}
