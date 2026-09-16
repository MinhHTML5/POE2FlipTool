using POE2FlipTool.DataModel;

namespace POE2FlipTool.Modules
{
    /// <summary>Result of one flip route: divine profit per divine invested, and per 1,000,000 gold of fees.</summary>
    public readonly struct RouteProfit
    {
        public static readonly RouteProfit None = new RouteProfit(null, null);

        /// <summary>Sheet T / X / AB / AF: div gained per div invested.</summary>
        public double? PerDiv { get; }
        /// <summary>Sheet F / H / L / N: div gained per 1,000,000 gold spent on exchange fees.</summary>
        public double? PerMillionGold { get; }

        public RouteProfit(double? perDiv, double? perMillionGold)
        {
            PerDiv = perDiv;
            PerMillionGold = perMillionGold;
        }
    }

    /// <summary>
    /// Port of the profit formulas in the "AutoFlipPOE2" sheet. Column letters in the comments
    /// refer to that sheet so the two can be compared side by side.
    ///
    /// Config cells: B2 = div->ex rate, B3 = div->chaos rate, B4 = gold per exalt,
    ///               B5 = gold per chaos, B6 = gold per div. Per item: B = gold cost.
    /// </summary>
    public static class ProfitCalculator
    {
        public const double GOLD_BUDGET = 1_000_000;

        /// <summary>Fills the eight Profit* fields on the reading. Missing inputs leave that route null.</summary>
        public static void Fill(ItemReading r, ExchangeRates rates)
        {
            RouteProfit exDiv = BuyWithOtherSellForDiv(r.GoldCost, r.SellForDiv, r.BuyWithEx, rates.DivToEx, rates.GoldPerEx, rates.GoldPerDiv);
            RouteProfit chaosDiv = BuyWithOtherSellForDiv(r.GoldCost, r.SellForDiv, r.BuyWithChaos, rates.DivToChaos, rates.GoldPerChaos, rates.GoldPerDiv);
            RouteProfit divEx = BuyWithDivSellForOther(r.GoldCost, r.BuyWithDiv, r.SellForEx, rates.DivToEx, rates.GoldPerEx, rates.GoldPerDiv);
            RouteProfit divChaos = BuyWithDivSellForOther(r.GoldCost, r.BuyWithDiv, r.SellForChaos, rates.DivToChaos, rates.GoldPerChaos, rates.GoldPerDiv);

            r.ProfitBuyExSellDiv = exDiv.PerMillionGold;
            r.ProfitBuyChaosSellDiv = chaosDiv.PerMillionGold;
            r.ProfitBuyDivSellEx = divEx.PerMillionGold;
            r.ProfitBuyDivSellChaos = divChaos.PerMillionGold;

            r.ProfitPerDivBuyExSellDiv = exDiv.PerDiv;
            r.ProfitPerDivBuyChaosSellDiv = chaosDiv.PerDiv;
            r.ProfitPerDivBuyDivSellEx = divEx.PerDiv;
            r.ProfitPerDivBuyDivSellChaos = divChaos.PerDiv;
        }

        /// <summary>
        /// Route: convert div to the other currency, buy the item with it, sell the item for div.
        /// Sheet columns R..U then F (exalt) or V..Y then H (chaos).
        /// </summary>
        /// <param name="goldCost">B: gold fee per item.</param>
        /// <param name="sellForDiv">D: div received per item.</param>
        /// <param name="buyWithOther">E or G: other currency paid per item.</param>
        /// <param name="divToOther">B2 or B3: other currency per div.</param>
        /// <param name="goldPerOther">B4 or B5: gold fee per unit of other currency.</param>
        /// <param name="goldPerDiv">B6: gold fee per div.</param>
        public static RouteProfit BuyWithOtherSellForDiv(double? goldCost, double? sellForDiv, double? buyWithOther,
                                                         double? divToOther, double? goldPerOther, double? goldPerDiv)
        {
            if (!AllPresent(goldCost, sellForDiv, buyWithOther, divToOther, goldPerOther, goldPerDiv)) return RouteProfit.None;
            if (divToOther == 0) return RouteProfit.None;

            double divCost = buyWithOther.Value / divToOther.Value;            // R = E / B2
            if (divCost == 0) return RouteProfit.None;
            double profitPerItem = sellForDiv.Value - divCost;                 // S = D - R
            double profitPerDiv = (1 / divCost) * profitPerItem;               // T = (1/R) * S
            double goldPerDivInvested =                                        // U = B2*B4 + (1/R)*B + (1+T)*B6
                divToOther.Value * goldPerOther.Value
                + (1 / divCost) * goldCost.Value
                + (1 + profitPerDiv) * goldPerDiv.Value;
            if (goldPerDivInvested == 0) return new RouteProfit(profitPerDiv, null);

            return new RouteProfit(profitPerDiv, GOLD_BUDGET / goldPerDivInvested * profitPerDiv); // F = 1000000/U * T
        }

        /// <summary>
        /// Route: buy the item with div, sell it for the other currency, convert back to div.
        /// Sheet columns Z..AC then L (exalt) or AD..AG then N (chaos).
        /// </summary>
        /// <param name="goldCost">B: gold fee per item.</param>
        /// <param name="buyWithDiv">J: div paid per item.</param>
        /// <param name="sellForOther">K or M: other currency received per item.</param>
        /// <param name="divToOther">B2 or B3: other currency per div.</param>
        /// <param name="goldPerOther">B4 or B5: gold fee per unit of other currency.</param>
        /// <param name="goldPerDiv">B6: gold fee per div.</param>
        public static RouteProfit BuyWithDivSellForOther(double? goldCost, double? buyWithDiv, double? sellForOther,
                                                         double? divToOther, double? goldPerOther, double? goldPerDiv)
        {
            if (!AllPresent(goldCost, buyWithDiv, sellForOther, divToOther, goldPerOther, goldPerDiv)) return RouteProfit.None;
            if (divToOther == 0 || buyWithDiv == 0) return RouteProfit.None;

            double divSold = sellForOther.Value / divToOther.Value;            // Z = K / B2
            double profitPerItem = divSold - buyWithDiv.Value;                 // AA = Z - J
            double itemsPerDiv = 1 / buyWithDiv.Value;
            double profitPerDiv = itemsPerDiv * profitPerItem;                 // AB = (1/J) * AA
            double goldPerDivInvested =                                        // AC = (1/J)*B + (1/J)*K*B4 + (1/J)*K/B2*B6
                itemsPerDiv * goldCost.Value
                + itemsPerDiv * sellForOther.Value * goldPerOther.Value
                + itemsPerDiv * sellForOther.Value / divToOther.Value * goldPerDiv.Value;
            if (goldPerDivInvested == 0) return new RouteProfit(profitPerDiv, null);

            return new RouteProfit(profitPerDiv, GOLD_BUDGET / goldPerDivInvested * profitPerDiv); // L = 1000000/AC * AB
        }

        private static bool AllPresent(params double?[] values)
        {
            return values.All(v => v.HasValue);
        }
    }
}
