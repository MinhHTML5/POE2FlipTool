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
        public static List<TradeItem> ReadConfig()
        {
            string json = File.ReadAllText("CONFIG.json");

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
