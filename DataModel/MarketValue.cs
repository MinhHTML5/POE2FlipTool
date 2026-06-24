namespace POE2FlipTool.DataModel
{
    public record MarketListing(float Rate, float Volume);

    public class MarketValue
    {
        public string ItemBuyName { get; set; } = "";
        public string ItemSellName { get; set; } = "";
        public List<MarketListing> AvailableRate { get; set; } = new();
        public List<MarketListing> CompetingRate { get; set; } = new();
    }

}
