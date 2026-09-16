using POE2FlipTool.DataModel;
using System.Globalization;

namespace POE2FlipTool.Modules
{
    /// <summary>
    /// The tool's in-memory copy of the sheet: the item list (from column A/B), the exchange rates
    /// (CONFIG block B2..B6) and the latest known price for every item. The grid on the main form
    /// displays this; the scanner and the history file feed it.
    /// </summary>
    public class PriceBoard
    {
        // CONFIG block of the sheet
        public const string DIV_TO_EX_CELL = "B2";
        public const string DIV_TO_CHAOS_CELL = "B3";
        public const int DIV_TO_EX_ROW = 2;
        public const int DIV_TO_CHAOS_ROW = 3;
        public const int GOLD_PER_EX_ROW = 4;
        public const int GOLD_PER_CHAOS_ROW = 5;
        public const int GOLD_PER_DIV_ROW = 6;

        public ExchangeRates Rates { get; } = new ExchangeRates();

        /// <summary>Items in sheet order. Each holds the latest known prices and profits.</summary>
        public List<ItemReading> Items { get; } = new List<ItemReading>();

        /// <summary>Category names in the order they appear in the sheet.</summary>
        public List<string> Categories { get; } = new List<string>();

        private readonly Dictionary<string, ItemReading> _byName = new Dictionary<string, ItemReading>();

        public ItemReading? Find(string name)
        {
            return _byName.TryGetValue(name, out var item) ? item : null;
        }

        /// <summary>
        /// A category header cell wraps its name in tildes, e.g. "!! ~ FRAGMENT ~ !!" -> "FRAGMENT".
        /// Whitespace around the name is ignored so "!! ~ RITUAL~ !!" still works.
        /// </summary>
        public static bool TryGetCategoryName(string value, out string name)
        {
            name = "";
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            int first = value.IndexOf('~');
            int last = value.LastIndexOf('~');
            if (first < 0 || last <= first)
            {
                return false;
            }

            name = value.Substring(first + 1, last - first - 1).Trim();
            return name.Length > 0;
        }

        /// <summary>Rows containing "!!" are config, headers or excluded items.</summary>
        public static bool IsItemRow(string value)
        {
            return value.Length > 0 && !value.Contains("!!");
        }

        /// <summary>
        /// Rebuilds the item list and rates from the sheet's A:B rows. Prices already known for an
        /// item that is still in the sheet are kept; items removed from the sheet are dropped.
        /// </summary>
        public void LoadFromSheet(List<(int Row, IList<object> Cells)> rows)
        {
            var sheetRates = new ExchangeRates();
            var newItems = new List<ItemReading>();
            Categories.Clear();
            string currentCategory = "";

            foreach (var (row, cells) in rows)
            {
                switch (row)
                {
                    case DIV_TO_EX_ROW: sheetRates.DivToEx = CellNumber(cells, 1); break;
                    case DIV_TO_CHAOS_ROW: sheetRates.DivToChaos = CellNumber(cells, 1); break;
                    case GOLD_PER_EX_ROW: sheetRates.GoldPerEx = CellNumber(cells, 1); break;
                    case GOLD_PER_CHAOS_ROW: sheetRates.GoldPerChaos = CellNumber(cells, 1); break;
                    case GOLD_PER_DIV_ROW: sheetRates.GoldPerDiv = CellNumber(cells, 1); break;
                }

                string value = CellString(cells, 0);
                if (TryGetCategoryName(value, out string categoryName))
                {
                    currentCategory = categoryName;
                    if (!Categories.Contains(categoryName)) Categories.Add(categoryName);
                    continue;
                }
                if (!IsItemRow(value))
                {
                    continue;
                }

                if (_byName.TryGetValue(value, out var existing))
                {
                    existing.Row = row;
                    existing.Category = currentCategory;
                    existing.GoldCost = CellNumber(cells, 1) ?? existing.GoldCost;
                    newItems.Add(existing);
                }
                else
                {
                    var item = new ItemReading(value, row, currentCategory) { GoldCost = CellNumber(cells, 1) };
                    _byName[value] = item;
                    newItems.Add(item);
                }
            }

            // Sheet values win; anything the sheet lacks keeps what we had (e.g. from history).
            sheetRates.FillMissingFrom(Rates);
            CopyRates(sheetRates, Rates);

            Items.Clear();
            Items.AddRange(newItems);
            foreach (string gone in _byName.Keys.Except(newItems.Select(i => i.Name)).ToList())
            {
                _byName.Remove(gone);
            }

            RecalculateAll();
        }

        /// <summary>
        /// Fills each item's prices from the history, newest record first. A price the newest record
        /// lacks (run cancelled early, OCR miss, currency unchecked) is taken from the next older record,
        /// and so on until every price is known or the history runs out.
        /// </summary>
        public void LoadFromHistory(List<HistoryRecord> records)
        {
            var byItem = records
                .GroupBy(r => r.Reading.Name)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.Reading.Timestamp).ToList());

            foreach (var item in Items)
            {
                if (!byItem.TryGetValue(item.Name, out var itemRecords)) continue;

                foreach (var record in itemRecords)
                {
                    item.FillMissingPricesFrom(record.Reading);
                    if (item.Timestamp == default)
                    {
                        item.Timestamp = record.Reading.Timestamp;
                    }
                    if (item.HasAllPrices()) break;
                }
            }

            // Rates the sheet did not provide fall back to the most recent record that has them.
            foreach (var record in records.OrderByDescending(r => r.Reading.Timestamp))
            {
                Rates.FillMissingFrom(record.Rates);
            }

            RecalculateAll();
        }

        /// <summary>
        /// Merges a fresh scan result into the board: prices the scan produced replace the old ones,
        /// prices it could not produce keep their previous value.
        /// </summary>
        public ItemReading Apply(ItemReading reading)
        {
            if (!_byName.TryGetValue(reading.Name, out var item))
            {
                item = new ItemReading(reading.Name, reading.Row, reading.Category);
                _byName[reading.Name] = item;
                Items.Add(item);
            }

            bool anyPrice = false;
            foreach (var field in PriceFields.All)
            {
                double? value = reading.Get(field);
                if (value.HasValue)
                {
                    item.Set(field, value);
                    anyPrice = true;
                }
            }
            if (reading.GoldCost.HasValue) item.GoldCost = reading.GoldCost;
            item.Row = reading.Row;
            item.Category = reading.Category;
            if (anyPrice)
            {
                // Only a real reading moves "Last read"; a fully failed pass leaves the old data and its time.
                item.Timestamp = reading.Timestamp;
            }

            Recalculate(item);
            return item;
        }

        public void Recalculate(ItemReading item)
        {
            ProfitCalculator.Fill(item, Rates);
        }

        public void RecalculateAll()
        {
            foreach (var item in Items) Recalculate(item);
        }

        private static void CopyRates(ExchangeRates from, ExchangeRates to)
        {
            to.DivToEx = from.DivToEx;
            to.DivToChaos = from.DivToChaos;
            to.GoldPerEx = from.GoldPerEx;
            to.GoldPerChaos = from.GoldPerChaos;
            to.GoldPerDiv = from.GoldPerDiv;
        }

        private static string CellString(IList<object> cells, int index)
        {
            if (index >= cells.Count || cells[index] == null) return "";
            return cells[index].ToString() ?? "";
        }

        private static double? CellNumber(IList<object> cells, int index)
        {
            if (index >= cells.Count || cells[index] == null) return null;
            object v = cells[index];
            if (v is string text)
            {
                return double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double parsed)
                    ? parsed : null;
            }
            if (v is IConvertible convertible)
            {
                try { return convertible.ToDouble(CultureInfo.InvariantCulture); }
                catch { return null; }
            }
            return null;
        }
    }
}
