using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POE2FlipTool.DataModel
{
    public class MarketValue
    {
        public string ItemBuyName { get; set; } = "";
        public string ItemSellName { get; set; } = "";
        public List<(float, float)> AvailableRate { get; set; } = new List<(float, float)>();
        public List<(float, float)> CompetingRate { get; set; } = new List<(float, float)>();
    }
}
