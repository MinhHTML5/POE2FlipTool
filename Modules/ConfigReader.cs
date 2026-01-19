using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace POE2FlipTool.Modules
{
    internal class ConfigReader
    {
        public static List<string> ReadStringArrayFromJsonFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("JSON file not found.", filePath);
            }

            string json = File.ReadAllText(filePath);

            List<string>? result = JsonSerializer.Deserialize<List<string>>(json);

            if (result == null)
            {
                throw new InvalidDataException("JSON content is not a valid string array.");
            }

            return result;
        }
    }
}
