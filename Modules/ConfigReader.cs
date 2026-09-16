using POE2FlipTool.DataModel;
using System.Text.Json;
using System.Text.Json.Serialization;

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
    }
}
