namespace POE2FlipTool.DataModel
{
    public record Rate(float Price, float Volume);

    public class MarketValue
    {
        public string ItemBuyName { get; set; } = "";
        public string ItemSellName { get; set; } = "";
        public List<Rate> AvailableRate { get; set; } = new();
        public List<Rate> CompetingRate { get; set; } = new();
    }

}
