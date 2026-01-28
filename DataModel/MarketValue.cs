namespace POE2FlipTool.DataModel
{
    public record MarketRecord(float Rate, float Volume);

    public class MarketValue
    {
        public string ItemBuyName { get; set; } = "";
        public string ItemSellName { get; set; } = "";
        public List<MarketRecord> AvailableRate { get; set; } = new();
        public List<MarketRecord> CompetingRate { get; set; } = new();
    }

}
