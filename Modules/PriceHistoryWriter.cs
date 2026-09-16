using POE2FlipTool.DataModel;
using System.Globalization;
using System.Text;

namespace POE2FlipTool.Modules
{
    /// <summary>One CSV line read back from the history: the item reading plus the rates in effect.</summary>
    public class HistoryRecord
    {
        public ItemReading Reading { get; }
        public ExchangeRates Rates { get; }

        public HistoryRecord(ItemReading reading, ExchangeRates rates)
        {
            Reading = reading;
            Rates = rates;
        }
    }

    /// <summary>
    /// Appends every item reading to a per-day CSV so price movement over the day can be charted later,
    /// and reads those files back on startup.
    /// Files live in history/(poe1|poe2)/prices_YYYY-MM-DD.csv next to the executable.
    /// </summary>
    public class PriceHistoryWriter
    {
        private readonly string _directory;

        private const string TIMESTAMP_FORMAT = "yyyy-MM-dd HH:mm:ss";

        private static readonly string[] COLUMNS =
        {
            "timestamp", "category", "item", "row", "gold_cost",
            "sell_for_div", "buy_with_ex", "buy_with_chaos", "buy_with_div", "sell_for_ex", "sell_for_chaos",
            "div_to_ex", "div_to_chaos", "gold_per_ex", "gold_per_chaos", "gold_per_div",
            "profit_1m_buy_ex_sell_div", "profit_1m_buy_chaos_sell_div",
            "profit_1m_buy_div_sell_ex", "profit_1m_buy_div_sell_chaos", "best_profit_1m",
            "profit_div_buy_ex_sell_div", "profit_div_buy_chaos_sell_div",
            "profit_div_buy_div_sell_ex", "profit_div_buy_div_sell_chaos"
        };

        public PriceHistoryWriter(string poeConfig)
        {
            _directory = Path.Combine("history", poeConfig);
        }

        public string Directory => _directory;

        public string CurrentFilePath(DateTime when)
        {
            return Path.Combine(_directory, $"prices_{when:yyyy-MM-dd}.csv");
        }

        public void Append(ItemReading reading, ExchangeRates rates)
        {
            System.IO.Directory.CreateDirectory(_directory);
            string path = CurrentFilePath(reading.Timestamp);
            bool writeHeader = !File.Exists(path) || new FileInfo(path).Length == 0;

            var sb = new StringBuilder();
            if (writeHeader)
            {
                sb.AppendLine(string.Join(",", COLUMNS));
            }

            string[] cells =
            {
                reading.Timestamp.ToString(TIMESTAMP_FORMAT, CultureInfo.InvariantCulture),
                Csv(reading.Category),
                Csv(reading.Name),
                reading.Row.ToString(CultureInfo.InvariantCulture),
                Num(reading.GoldCost),
                Num(reading.SellForDiv), Num(reading.BuyWithEx), Num(reading.BuyWithChaos),
                Num(reading.BuyWithDiv), Num(reading.SellForEx), Num(reading.SellForChaos),
                Num(rates.DivToEx), Num(rates.DivToChaos), Num(rates.GoldPerEx), Num(rates.GoldPerChaos), Num(rates.GoldPerDiv),
                Num(reading.ProfitBuyExSellDiv), Num(reading.ProfitBuyChaosSellDiv),
                Num(reading.ProfitBuyDivSellEx), Num(reading.ProfitBuyDivSellChaos),
                Num(reading.BestProfit()),
                Num(reading.ProfitPerDivBuyExSellDiv), Num(reading.ProfitPerDivBuyChaosSellDiv),
                Num(reading.ProfitPerDivBuyDivSellEx), Num(reading.ProfitPerDivBuyDivSellChaos)
            };
            sb.AppendLine(string.Join(",", cells));

            File.AppendAllText(path, sb.ToString(), new UTF8Encoding(false));
        }

        /// <summary>
        /// Reads every history file and returns all records, oldest first.
        /// Columns are looked up by header name so older files with fewer columns still load.
        /// </summary>
        public List<HistoryRecord> ReadAll()
        {
            var result = new List<HistoryRecord>();
            if (!System.IO.Directory.Exists(_directory))
            {
                return result;
            }

            foreach (string file in System.IO.Directory.EnumerateFiles(_directory, "prices_*.csv"))
            {
                ReadFile(file, result);
            }

            result.Sort((a, b) => a.Reading.Timestamp.CompareTo(b.Reading.Timestamp));
            return result;
        }

        /// <summary>Reads only the file for one calendar day, oldest first. Empty when there is no file.</summary>
        public List<HistoryRecord> ReadDay(DateTime day)
        {
            var result = new List<HistoryRecord>();
            string path = CurrentFilePath(day);
            if (File.Exists(path))
            {
                ReadFile(path, result);
                result.Sort((a, b) => a.Reading.Timestamp.CompareTo(b.Reading.Timestamp));
            }
            return result;
        }

        private static void ReadFile(string file, List<HistoryRecord> into)
        {
            string[] lines = File.ReadAllLines(file);
            if (lines.Length < 2) return;

            List<string> header = ParseCsvLine(lines[0]);
            var index = new Dictionary<string, int>();
            for (int i = 0; i < header.Count; i++) index[header[i]] = i;

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                List<string> cells = ParseCsvLine(lines[i]);

                string Col(string name) =>
                    index.TryGetValue(name, out int idx) && idx < cells.Count ? cells[idx] : "";

                if (!DateTime.TryParseExact(Col("timestamp"), TIMESTAMP_FORMAT, CultureInfo.InvariantCulture,
                                            DateTimeStyles.None, out DateTime timestamp))
                {
                    continue;
                }

                int.TryParse(Col("row"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int row);
                var reading = new ItemReading(Col("item"), row, Col("category"))
                {
                    Timestamp = timestamp,
                    GoldCost = Num(Col("gold_cost")),
                    SellForDiv = Num(Col("sell_for_div")),
                    BuyWithEx = Num(Col("buy_with_ex")),
                    BuyWithChaos = Num(Col("buy_with_chaos")),
                    BuyWithDiv = Num(Col("buy_with_div")),
                    SellForEx = Num(Col("sell_for_ex")),
                    SellForChaos = Num(Col("sell_for_chaos")),
                    ProfitBuyExSellDiv = Num(Col("profit_1m_buy_ex_sell_div")),
                    ProfitBuyChaosSellDiv = Num(Col("profit_1m_buy_chaos_sell_div")),
                    ProfitBuyDivSellEx = Num(Col("profit_1m_buy_div_sell_ex")),
                    ProfitBuyDivSellChaos = Num(Col("profit_1m_buy_div_sell_chaos")),
                    ProfitPerDivBuyExSellDiv = Num(Col("profit_div_buy_ex_sell_div")),
                    ProfitPerDivBuyChaosSellDiv = Num(Col("profit_div_buy_chaos_sell_div")),
                    ProfitPerDivBuyDivSellEx = Num(Col("profit_div_buy_div_sell_ex")),
                    ProfitPerDivBuyDivSellChaos = Num(Col("profit_div_buy_div_sell_chaos")),
                };
                var rates = new ExchangeRates
                {
                    DivToEx = Num(Col("div_to_ex")),
                    DivToChaos = Num(Col("div_to_chaos")),
                    GoldPerEx = Num(Col("gold_per_ex")),
                    GoldPerChaos = Num(Col("gold_per_chaos")),
                    GoldPerDiv = Num(Col("gold_per_div")),
                };
                into.Add(new HistoryRecord(reading, rates));
            }
        }

        private static string Num(double? value)
        {
            return value.HasValue ? value.Value.ToString("0.####", CultureInfo.InvariantCulture) : "";
        }

        private static double? Num(string text)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : null;
        }

        private static string Csv(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }

        /// <summary>Minimal RFC 4180 line parser: handles quoted fields and doubled quotes.</summary>
        private static List<string> ParseCsvLine(string line)
        {
            var cells = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            current.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    cells.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            cells.Add(current.ToString());
            return cells;
        }
    }
}
