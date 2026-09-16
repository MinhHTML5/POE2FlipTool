namespace POE2FlipTool.DataModel
{
    /// <summary>The six prices the tool reads per item. Names match the sheet headers.</summary>
    public enum PriceField
    {
        SellForDiv,     // D
        BuyWithEx,      // E
        BuyWithChaos,   // G
        BuyWithDiv,     // J
        SellForEx,      // K
        SellForChaos    // M
    }

    public static class PriceFields
    {
        public static readonly PriceField[] All = (PriceField[])Enum.GetValues(typeof(PriceField));

        /// <summary>Sheet column that stores this price.</summary>
        public static string SheetColumn(PriceField field)
        {
            switch (field)
            {
                case PriceField.SellForDiv: return "D";
                case PriceField.BuyWithEx: return "E";
                case PriceField.BuyWithChaos: return "G";
                case PriceField.BuyWithDiv: return "J";
                case PriceField.SellForEx: return "K";
                case PriceField.SellForChaos: return "M";
                default: throw new ArgumentOutOfRangeException(nameof(field));
            }
        }

        /// <summary>Header text, same wording as the sheet.</summary>
        public static string Header(PriceField field)
        {
            switch (field)
            {
                case PriceField.SellForDiv: return "Sell for div";
                case PriceField.BuyWithEx: return "Buy with ex";
                case PriceField.BuyWithChaos: return "Buy with chaos";
                case PriceField.BuyWithDiv: return "Buy with div";
                case PriceField.SellForEx: return "Sell for ex";
                case PriceField.SellForChaos: return "Sell for chaos";
                default: throw new ArgumentOutOfRangeException(nameof(field));
            }
        }
    }

    /// <summary>
    /// One full pass over a single item on the currency exchange, plus the derived profit numbers.
    /// Every price is expressed as "currency per 1 item", matching the sheet columns D/E/G/J/K/M.
    /// A null price means the OCR could not read that value (or that currency was not checked).
    /// </summary>
    public class ItemReading
    {
        public DateTime Timestamp { get; set; }
        public string Category { get; set; } = "";
        public string Name { get; set; } = "";
        public int Row { get; set; }

        /// <summary>Sheet column B: gold fee charged by the exchange for trading this item.</summary>
        public double? GoldCost { get; set; }

        /// <summary>Column D: div you receive per item when selling it for divine.</summary>
        public double? SellForDiv { get; set; }
        /// <summary>Column E: exalt you pay per item when buying with exalt.</summary>
        public double? BuyWithEx { get; set; }
        /// <summary>Column G: chaos you pay per item when buying with chaos.</summary>
        public double? BuyWithChaos { get; set; }
        /// <summary>Column J: div you pay per item when buying with divine.</summary>
        public double? BuyWithDiv { get; set; }
        /// <summary>Column K: exalt you receive per item when selling for exalt.</summary>
        public double? SellForEx { get; set; }
        /// <summary>Column M: chaos you receive per item when selling for chaos.</summary>
        public double? SellForChaos { get; set; }

        // Divine profit per 1,000,000 gold spent on exchange fees, one per flip route.
        /// <summary>Column F: buy with exalt, sell for divine.</summary>
        public double? ProfitBuyExSellDiv { get; set; }
        /// <summary>Column H: buy with chaos, sell for divine.</summary>
        public double? ProfitBuyChaosSellDiv { get; set; }
        /// <summary>Column L: buy with divine, sell for exalt.</summary>
        public double? ProfitBuyDivSellEx { get; set; }
        /// <summary>Column N: buy with divine, sell for chaos.</summary>
        public double? ProfitBuyDivSellChaos { get; set; }

        // Divine profit per 1 divine invested, one per flip route (the sheet's helper columns T / X / AB / AF).
        public double? ProfitPerDivBuyExSellDiv { get; set; }
        public double? ProfitPerDivBuyChaosSellDiv { get; set; }
        public double? ProfitPerDivBuyDivSellEx { get; set; }
        public double? ProfitPerDivBuyDivSellChaos { get; set; }

        public ItemReading(string name, int row, string category)
        {
            Name = name;
            Row = row;
            Category = category;
        }

        public double? Get(PriceField field)
        {
            switch (field)
            {
                case PriceField.SellForDiv: return SellForDiv;
                case PriceField.BuyWithEx: return BuyWithEx;
                case PriceField.BuyWithChaos: return BuyWithChaos;
                case PriceField.BuyWithDiv: return BuyWithDiv;
                case PriceField.SellForEx: return SellForEx;
                case PriceField.SellForChaos: return SellForChaos;
                default: throw new ArgumentOutOfRangeException(nameof(field));
            }
        }

        public void Set(PriceField field, double? value)
        {
            switch (field)
            {
                case PriceField.SellForDiv: SellForDiv = value; break;
                case PriceField.BuyWithEx: BuyWithEx = value; break;
                case PriceField.BuyWithChaos: BuyWithChaos = value; break;
                case PriceField.BuyWithDiv: BuyWithDiv = value; break;
                case PriceField.SellForEx: SellForEx = value; break;
                case PriceField.SellForChaos: SellForChaos = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(field));
            }
        }

        public bool HasAllPrices()
        {
            return PriceFields.All.All(f => Get(f).HasValue);
        }

        /// <summary>Copies every price that is still null here from <paramref name="older"/>.</summary>
        public void FillMissingPricesFrom(ItemReading older)
        {
            foreach (var field in PriceFields.All)
            {
                if (!Get(field).HasValue)
                {
                    Set(field, older.Get(field));
                }
            }
            if (!GoldCost.HasValue) GoldCost = older.GoldCost;
        }

        /// <summary>Highest of the four route profits, or null when none could be computed.</summary>
        public double? BestProfit()
        {
            double?[] all = { ProfitBuyExSellDiv, ProfitBuyChaosSellDiv, ProfitBuyDivSellEx, ProfitBuyDivSellChaos };
            var present = all.Where(p => p.HasValue).Select(p => p.Value).ToList();
            return present.Count > 0 ? present.Max() : null;
        }
    }

    /// <summary>
    /// The exchange rates and gold fees from the sheet's CONFIG block (B2..B6).
    /// </summary>
    public class ExchangeRates
    {
        /// <summary>B2: exalts per 1 divine.</summary>
        public double? DivToEx { get; set; }
        /// <summary>B3: chaos per 1 divine.</summary>
        public double? DivToChaos { get; set; }
        /// <summary>B4: gold fee per exalt traded.</summary>
        public double? GoldPerEx { get; set; }
        /// <summary>B5: gold fee per chaos traded.</summary>
        public double? GoldPerChaos { get; set; }
        /// <summary>B6: gold fee per divine traded.</summary>
        public double? GoldPerDiv { get; set; }

        /// <summary>Copies every rate that is still null here from <paramref name="other"/>.</summary>
        public void FillMissingFrom(ExchangeRates other)
        {
            DivToEx ??= other.DivToEx;
            DivToChaos ??= other.DivToChaos;
            GoldPerEx ??= other.GoldPerEx;
            GoldPerChaos ??= other.GoldPerChaos;
            GoldPerDiv ??= other.GoldPerDiv;
        }
    }
}
