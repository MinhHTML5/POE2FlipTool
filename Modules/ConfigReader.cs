using POE2FlipTool.DataModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ninja.poe;

namespace POE2FlipTool.Modules
{
    internal class ConfigReader
    {
        public static string poeConfig = "poe1";
        public static GeneralConfig ReadGeneralConfig()
        {
            string json = File.ReadAllText("data/config/" + poeConfig + "/GeneralConfig.json");
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
            GeneralConfig config = JsonSerializer.Deserialize<GeneralConfig>(json, options);
            return config;
        }

        public static List<TradeCategory> ReadCategoryConfig()
        {
            string json = File.ReadAllText("data/config/" + poeConfig + "/CategoryConfig.json");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            List<TradeCategory> categories = JsonSerializer.Deserialize<List<TradeCategory>>(json, options);

            return categories;
        }

        public static List<TradeItem> ReadItemConfig()
        {
            string json = File.ReadAllText("data/config/" + poeConfig + "/ItemConfig.json");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            List<TradeItem> items = JsonSerializer.Deserialize<List<TradeItem>>(json, options);

            return items;
        }

        public static List<TradedItem>? GetPoeNinjaList(string poeVersion)
        {
            var ninja = new Poe2Ninja(Console.WriteLine);
            return ninja.Economy.GetTradedItemsAboveVolume(1000, Poe2EconomyType.Currency | Poe2EconomyType.Fragments | Poe2EconomyType.Breach | Poe2EconomyType.Abyss | Poe2EconomyType.UncutGems)?.ToList();
        }
    }
}
