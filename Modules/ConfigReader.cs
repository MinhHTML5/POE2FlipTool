using POE2FlipTool.DataModel;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace POE2FlipTool.Modules
{
    internal class ConfigReader
    {
        public static string poeConfig = "poe1";
        public static GeneralConfig ReadGeneralConfig()
        {
            string json = File.ReadAllText("config/" + poeConfig + "/GeneralConfig.json");
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
            string json = File.ReadAllText("config/" + poeConfig + "/CategoryConfig.json");

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
            string json = File.ReadAllText("config/" + poeConfig + "/ItemConfig.json");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            List<TradeItem> items = JsonSerializer.Deserialize<List<TradeItem>>(json, options);

            return items;
        }
    }
}
